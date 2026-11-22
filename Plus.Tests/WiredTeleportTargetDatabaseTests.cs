using System.Data;
using Dapper;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Items;
using Xunit;

namespace Plus.Tests;

public sealed class WiredTeleportTargetDatabaseTests
{
    [RoomComponentDatabaseFact]
    public void RelinkingPersistsOnlyOneHalfAndRequiresPlacedMatchingItems()
    {
        var root = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        using var admin = new MySqlConnection(root);
        admin.Open();
        var schema = "task_wired_teleport_" + Guid.NewGuid().ToString("N");
        admin.Execute($"CREATE DATABASE `{schema}`");

        try {
            var options = new MySqlConnectionStringBuilder(root) { Database = schema };
            using var connection = new MySqlConnection(options.ConnectionString);
            connection.Open();
            connection.Execute("""
                CREATE TABLE furniture (id INT UNSIGNED PRIMARY KEY,interaction_type VARCHAR(25) NOT NULL);
                CREATE TABLE items (id INT UNSIGNED PRIMARY KEY,room_id INT UNSIGNED NOT NULL,base_item INT UNSIGNED NOT NULL);
                CREATE TABLE room_items_tele_links (id INT AUTO_INCREMENT PRIMARY KEY,tele_one_id INT UNSIGNED NOT NULL,tele_two_id INT UNSIGNED NOT NULL,KEY tele_one_id(tele_one_id));
                INSERT INTO furniture VALUES (1,'teleport'),(2,'teleport'),(3,'wired_room_linker'),(4,'default');
                INSERT INTO items VALUES (10,1,1),(20,1,1),(30,1,2),(40,2,1),(50,1,3),(60,1,4);
                INSERT INTO room_items_tele_links (tele_one_id,tele_two_id) VALUES (10,20),(20,10);
                """);
            IItemTravelStore store = new ItemTravelStore(new Database(options.ConnectionString));
            Assert.True(store.SetLinkedTeleporter(10, 1, 30));
            var reloaded = new ItemTravelStore(new Database(options.ConnectionString));
            Assert.Equal(30u, reloaded.FindLinkedTeleporter(10));
            Assert.Equal(10u, reloaded.FindLinkedTeleporter(20));

            foreach (var changedRows in new[] { false, true }) {
                var sameTargetOptions = new MySqlConnectionStringBuilder(options.ConnectionString) { UseAffectedRows = changedRows };
                IItemTravelStore sameTarget = new ItemTravelStore(new Database(sameTargetOptions.ConnectionString));
                Assert.True(sameTarget.SetLinkedTeleporter(10, 1, 30));
                Assert.True(sameTarget.SetLinkedTeleporter(10, 1, 30));
                Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM room_items_tele_links WHERE tele_one_id=10"));
            }

            Assert.False(store.SetLinkedTeleporter(10, 2, 20));
            Assert.False(store.SetLinkedTeleporter(10, 1, 0));
            Assert.False(store.SetLinkedTeleporter(999, 1, 20));

            foreach (var invalid in new uint[] { 40, 50, 60, 999 }) {
                Assert.False(store.SetLinkedTeleporter(10, 1, invalid));
                Assert.Equal(30u, reloaded.FindLinkedTeleporter(10));
            }

            Assert.False(store.SetLinkedTeleporter(60, 1, 60));
            Assert.True(store.SetLinkedTeleporter(50, 1, 50));
            Assert.Equal((50u, 1u), store.FindTravelDestination(50, 1));
            Assert.Equal((30u, 1u), store.FindTravelDestination(10, 1));
            connection.Execute("UPDATE items SET room_id=2 WHERE id=30");
            Assert.Equal(default, store.FindTravelDestination(10, 1));
            connection.Execute("INSERT INTO room_items_tele_links (tele_one_id,tele_two_id) VALUES (30,10)");
            Assert.Equal((30u, 2u), store.FindTravelDestination(10, 1));
            Assert.Equal(default, store.FindTravelDestination(10, 2));
            connection.Execute("UPDATE items SET room_id=0 WHERE id=30");
            Assert.Equal(default, store.FindTravelDestination(10, 1));
            connection.Execute("UPDATE items SET room_id=2, base_item=4 WHERE id=30");
            Assert.Equal(default, store.FindTravelDestination(10, 1));
            connection.Execute("DELETE FROM items WHERE id=30");
            Assert.Equal(default, store.FindTravelDestination(10, 1));
            connection.Execute("""
                CREATE TRIGGER reject_link_update BEFORE UPDATE ON room_items_tele_links FOR EACH ROW
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='isolated link update failure';
                """);
            Assert.Throws<MySqlException>(() => store.SetLinkedTeleporter(10, 1, 20));
            Assert.Equal(30u, reloaded.FindLinkedTeleporter(10));
        }
        finally {
            admin.Execute($"DROP DATABASE `{schema}`");
        }
    }
    private sealed class Database(string connectionString) : IDatabase
    {
        public bool IsConnected() => true;
        public IDbConnection Connection() => new MySqlConnection(connectionString);
    }
}
