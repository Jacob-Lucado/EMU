using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

public sealed class GroupFurniStartStateMigrationTests
{
    private const string Migration = "Database/Migrations/167_GroupFurniStartState.sql";

    [RoomComponentDatabaseFact]
    public void GroupFurniThatStoreTheirGroupIdAsStateStartInStateZero()
    {
        var server = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = "group_furni_state_" + Guid.NewGuid().ToString("N");

        using (var setup = new MySqlConnection(server)) {
            setup.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server) { Database = schema }.ConnectionString);
            connection.Open();
            connection.Execute("""
                CREATE TABLE furniture (id INT UNSIGNED NOT NULL, item_name VARCHAR(70) NOT NULL, interaction_type VARCHAR(25) NOT NULL DEFAULT 'default',
                    interaction_modes_count INT NOT NULL DEFAULT 1, PRIMARY KEY (id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                CREATE TABLE items (id INT UNSIGNED NOT NULL AUTO_INCREMENT, user_id INT NOT NULL, room_id INT NOT NULL DEFAULT 0, base_item INT UNSIGNED NOT NULL,
                    extra_data TEXT NOT NULL, PRIMARY KEY (id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                CREATE TABLE items_groups (id INT UNSIGNED NOT NULL, group_id INT UNSIGNED NOT NULL DEFAULT 0, PRIMARY KEY (id))
                    ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO furniture (id, item_name, interaction_type, interaction_modes_count) VALUES
                    (1, 'gld_gate', 'gld_gate', 4), (2, 'gld_table1', 'gld_item', 1), (3, 'guild_forum', 'guild_forum', 5),
                    (4, 'gld_badgewall_tall', 'gld_item', 11), (5, 'chair', 'default', 4);
                INSERT INTO items (id, user_id, room_id, base_item, extra_data) VALUES
                    (10, 1, 0, 1, '2'), (11, 1, 97, 2, '11'), (12, 1, 0, 3, '4'), (13, 1, 0, 4, '9'), (14, 1, 0, 1, '0'),
                    (15, 1, 90, 3, '1'), (16, 1, 0, 5, '3'), (17, 1, 0, 4, '');
                INSERT INTO items_groups (id, group_id) VALUES (10, 2), (11, 11), (12, 4), (13, 7), (14, 11), (16, 3), (17, 5);
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));

            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(new[] { "0", "0", "0", "9", "0", "1", "3", "" }, connection.Query<string>("SELECT extra_data FROM items ORDER BY id"));
            Assert.Equal(7, connection.QuerySingle<int>("SELECT COUNT(*) FROM items_groups"));
        }
        finally {
            using var cleanup = new MySqlConnection(server);
            cleanup.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
