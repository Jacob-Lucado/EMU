using System.Data;
using Dapper;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Groups;
using Xunit;

namespace Plus.Tests;

public sealed class GroupBadgePartMigrationTests
{
    private const string Migration = "Database/Migrations/166_GroupBadgePartIdsLikeHabbo.sql";
    private const string EditorIdMigration = "Database/Migrations/168_GroupBadgeEditorIds.sql";

    [RoomComponentDatabaseFact]
    public void ExistingBadgesKeepTheirPartsAndUseHabboBadgeCodes()
    {
        WithSchema("group_badge_ids_", connection =>
        {
            connection.Execute("""
                CREATE TABLE groups_items (type ENUM('base','symbol','color','color2','color3') NOT NULL, id INT NOT NULL,
                    firstvalue VARCHAR(255) NOT NULL, secondvalue VARCHAR(2000) NOT NULL, enabled TINYINT(1) NOT NULL DEFAULT 1,
                    PRIMARY KEY (id, type)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                CREATE TABLE `groups` (id INT UNSIGNED NOT NULL AUTO_INCREMENT, badge VARCHAR(50) NOT NULL, PRIMARY KEY (id))
                    ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO groups_items (type, id, firstvalue, secondvalue) VALUES
                    ('base', 1, 'base_basic_1.gif', ''), ('base', 22, 'base_ring.gif', ''),
                    ('symbol', 1, 'symbol_background_1.gif', ''), ('symbol', 68, 'symbol_alligator.gif', ''),
                    ('symbol', 183, 'symbol_a.gif', ''), ('symbol', 210, 'symbol_credit_part2.gif', 'symbol_credit_part1.gif'),
                    ('symbol', 120, 'symbol_custom.gif', ''), ('symbol', 250, 'symbol_custom_2.gif', ''),
                    ('symbol', 100104, 'symbol_unused.gif', '');
                INSERT INTO `groups` (id, badge) VALUES
                    (1, 'b01130s183104'), (2, 'b22030s68044s01082'), (3, 'b01010s210000'), (4, 'b01010s120001s250002'),
                    (5, 'b01010'), (6, 'not a badge'), (7, 'b0101s6804'), (8, 'b01010t110000');
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));

            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(new[] { "b01130s78104", "b22030t04044s01082", "b01010t14000", "b01010t59001s250002", "b01010", "not a badge", "b01010t04040", "b01010t14000" },
                connection.Query<string>("SELECT badge FROM `groups` ORDER BY id"));
            Assert.Equal(new[] { "symbol_a.gif", "symbol_alligator.gif", "symbol_credit_part2.gif", "symbol_custom.gif", "symbol_custom_2.gif" },
                connection.Query<string>("SELECT firstvalue FROM groups_items WHERE type = 'symbol' AND id IN (78, 104, 114, 159, 250) ORDER BY id = 250, id"));
            Assert.Equal("symbol_unused.gif", connection.QuerySingle<string>("SELECT firstvalue FROM groups_items WHERE type = 'symbol' AND id = 100104"));
            Assert.Equal("symbol_credit_part1.gif", connection.QuerySingle<string>("SELECT secondvalue FROM groups_items WHERE type = 'symbol' AND id = 114"));
            Assert.Equal("symbol_hc_part2.gif", connection.QuerySingle<string>("SELECT firstvalue FROM groups_items WHERE type = 'symbol' AND id = 158"));
        });
    }

    [RoomComponentDatabaseFact]
    public void TheBadgeEditorListsPartsAndColoursInTheirStoredOrder()
    {
        WithSchema("group_badge_order_", connection =>
        {
            connection.Execute("""
                CREATE TABLE groups_items (type ENUM('base','symbol','color','color2','color3') NOT NULL, id INT NOT NULL,
                    firstvalue VARCHAR(255) NOT NULL, secondvalue VARCHAR(2000) NOT NULL, enabled TINYINT(1) NOT NULL DEFAULT 1,
                    order_num INT NOT NULL DEFAULT 0, editor_id INT NULL, PRIMARY KEY (id, type)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO groups_items (type, id, firstvalue, secondvalue, order_num) VALUES
                    ('symbol', 1, 'symbol_a.gif', '', 3), ('symbol', 2, 'symbol_b.gif', '', 1), ('symbol', 3, 'symbol_c.gif', '', 2),
                    ('base', 1, 'base_a.gif', '', 2), ('base', 2, 'base_b.gif', '', 1),
                    ('color', 1, 'ffd601', '', 1), ('color', 2, 'ec7600', '', 3), ('color', 11, 'ffffff', '', 2);
                """);
            var groups = new GroupManager(TestLogging.For<GroupManager>(), new RawDatabase(connection.ConnectionString), null!, TimeProvider.System);

            groups.Init();

            Assert.Equal(new[] { 2, 3, 1 }, groups.BadgeSymbols.Select(part => part.Id));
            Assert.Equal(new[] { 2, 1 }, groups.BadgeBases.Select(part => part.Id));
            Assert.Equal(new[] { 1, 11, 2 }, groups.BadgeBaseColours.Select(colour => colour.Id));
        });
    }

    [RoomComponentDatabaseFact]
    public void HabboPartsGetTheirBadgeEditorIds()
    {
        WithSchema("group_badge_editor_ids_", connection =>
        {
            connection.Execute("""
                CREATE TABLE groups_items (type ENUM('base','symbol','color','color2','color3') NOT NULL, id INT NOT NULL,
                    firstvalue VARCHAR(255) NOT NULL, secondvalue VARCHAR(2000) NOT NULL, enabled TINYINT(1) NOT NULL DEFAULT 1,
                    order_num INT NOT NULL DEFAULT 0, PRIMARY KEY (id, type)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO groups_items (type, id, firstvalue, secondvalue) VALUES
                    ('base', 1, 'base_basic_1.gif', ''), ('base', 21, 'base_beams_part2.gif', 'base_beams_part1.gif'),
                    ('base', 29, 'base_custom.gif', ''), ('symbol', 1, 'symbol_background_1.gif', ''),
                    ('symbol', 68, 'symbol_0.gif', ''), ('symbol', 158, 'symbol_hc_part2.gif', 'symbol_hc_part1.gif'),
                    ('symbol', 3, 'SYMBOL_BACKGROUND_3_PART2.gif', ''), ('symbol', 2, 'symbol_renamed.gif', ''), ('symbol', 159, 'symbol_custom.gif', ''),
                    ('color', 1, 'ffd601', '');
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(EditorIdMigration));

            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(new (string, int, int?)[]
                {
                    ("base", 1, 1), ("base", 21, 65), ("base", 29, null),
                    ("symbol", 1, 21), ("symbol", 2, null), ("symbol", 3, null), ("symbol", 68, 173), ("symbol", 158, 211), ("symbol", 159, null),
                    ("color", 1, null)
                },
                connection.Query<(string, int, int?)>("SELECT type, id, editor_id FROM groups_items ORDER BY type, id"));
        });
    }

    [RoomComponentDatabaseFact]
    public void PartsWithoutAnEditorIdUseTheirIdPlusOneThousandAndClashesAreLeftOut()
    {
        WithSchema("group_badge_editor_load_", connection =>
        {
            connection.Execute("""
                CREATE TABLE groups_items (type ENUM('base','symbol','color','color2','color3') NOT NULL, id INT NOT NULL,
                    firstvalue VARCHAR(255) NOT NULL, secondvalue VARCHAR(2000) NOT NULL, enabled TINYINT(1) NOT NULL DEFAULT 1,
                    order_num INT NOT NULL DEFAULT 0, editor_id INT NULL, PRIMARY KEY (id, type)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                INSERT INTO groups_items (type, id, firstvalue, secondvalue, order_num, editor_id) VALUES
                    ('base', 21, 'base_beams_part2.gif', '', 1, 65), ('base', 29, 'base_custom.gif', '', 2, NULL),
                    ('symbol', 1, 'symbol_background_1.gif', '', 1, 21), ('symbol', 159, 'symbol_custom.gif', '', 2, NULL),
                    ('symbol', 160, 'symbol_clash.gif', '', 3, 1159), ('symbol', 161, 'symbol_free.gif', '', 4, 1300);
                """);
            var groups = new GroupManager(TestLogging.For<GroupManager>(), new RawDatabase(connection.ConnectionString), null!, TimeProvider.System);

            groups.Init();

            Assert.Equal(new[] { (21, 65), (29, 1029) }, groups.BadgeBases.Select(part => (part.Id, part.EditorId)));
            Assert.Equal(new[] { (1, 21), (159, 1159), (161, 1300) }, groups.BadgeSymbols.Select(part => (part.Id, part.EditorId)));
            Assert.True(groups.BadgePartIds.TryGetCodeId(false, 1300, out var custom));
            Assert.Equal(161, custom);
            Assert.Equal(1029, groups.BadgePartIds.GetEditorId(true, 29));
            Assert.Equal(0, groups.BadgePartIds.GetEditorId(false, 300));
        });
    }

    private static void WithSchema(string prefix, Action<MySqlConnection> test)
    {
        var server = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = prefix + Guid.NewGuid().ToString("N");

        using (var setup = new MySqlConnection(server)) {
            setup.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server) { Database = schema }.ConnectionString);
            connection.Open();
            test(connection);
        }
        finally {
            using var cleanup = new MySqlConnection(server);
            cleanup.Execute($"DROP DATABASE `{schema}`");
        }
    }

    private sealed class RawDatabase(string connectionString) : IDatabase
    {
        public IDbConnection Connection() => new MySqlConnection(connectionString);
        public bool IsConnected() => true;
    }
}
