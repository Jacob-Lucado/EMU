using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

public sealed class GroupFurniGiftMigrationTests
{
    private const string Migration = "Database/Migrations/169_GroupFurniGiftable.sql";

    [RoomComponentDatabaseFact]
    public void EveryGroupFurniAllowsGifts()
    {
        var server = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = "group_furni_gift_" + Guid.NewGuid().ToString("N");

        using (var setup = new MySqlConnection(server)) {
            setup.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server) { Database = schema }.ConnectionString);
            connection.Open();
            connection.Execute("""
                CREATE TABLE furniture (id INT UNSIGNED NOT NULL, item_name VARCHAR(70) NOT NULL, interaction_type VARCHAR(25) NOT NULL DEFAULT 'default',
                    allow_gift TINYINT(1) NOT NULL DEFAULT 1, PRIMARY KEY (id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO furniture (id, item_name, interaction_type, allow_gift) VALUES
                    (1, 'gld_gate', 'gld_gate', 0), (2, 'gld_stool2', 'gld_item', 0), (3, 'gld_stool1', 'gld_item', 0),
                    (4, 'fball_grp_fnc1', 'gld_item', 1), (5, 'guild_forum', 'guild_forum', 0), (6, 'chair', 'default', 0),
                    (7, 'rare_dragonlamp', 'default', 1);
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));

            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(new[] { 1, 1, 1, 1, 1, 0, 1 }, connection.Query<int>("SELECT allow_gift FROM furniture ORDER BY id"));
        }
        finally {
            using var cleanup = new MySqlConnection(server);
            cleanup.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
