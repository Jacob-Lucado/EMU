using Dapper;
using MySqlConnector;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired.Configuration;
using Xunit;

namespace Plus.Tests;

/// <summary>Migration 172: furniture rows for the group wired boxes, sold on the Wired pages of both catalogues.</summary>
public sealed class WiredGroupBoxesMigrationTests
{
    private const string Migration = "Database/Migrations/172_WiredGroupBoxes.sql";

    [RoomComponentDatabaseFact]
    public void TheBoxesGetRowsWithoutFurnidataThatBorrowThePlaceholderLook()
    {
        WithCatalogue(true, connection =>
        {
            var rows = connection.Query<(string Name, int Sprite, string Interaction, int Furnidata)>(
                "SELECT item_name, sprite_id, interaction_type, has_furnidata FROM furniture WHERE item_name NOT LIKE 'wf_test_%' ORDER BY item_name").ToArray();

            Assert.Equal(new[]
            {
                ("wf_act_invite_to_group", 19512, "wired_effect", 0),
                ("wf_act_remove_from_group", 19513, "wired_effect", 0),
                ("wf_trg_group_membership", 19514, "wired_trigger", 0)
            }, rows);
            Assert.All(rows, row =>
            {
                Assert.True(WiredBoxRegistry.TryGet(row.Name, out _), row.Name);
                Assert.NotEqual(InteractionType.None, InteractionTypes.GetTypeFromString(row.Interaction));
            });
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT has_furnidata FROM furniture WHERE item_name = 'wf_test_26secret3'"));
        });
    }

    [RoomComponentDatabaseFact]
    public void EveryWiredPageOfTheKindSellsOneOfferAndARerunChangesNothing()
    {
        WithCatalogue(true, connection =>
        {
            var listed = Listing(connection);

            Assert.Equal(new[]
            {
                (1, 5, "wf_act_invite_to_group", 7), (1, 6, "wf_act_remove_from_group", 7),
                (2, 0, "wf_act_invite_to_group", 7), (2, 1, "wf_act_remove_from_group", 7),
                (3, 0, "wf_trg_group_membership", 5)
            }, listed);
            Assert.Equal(3, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_offers WHERE localization_key <> 'old'"));
            Assert.Equal(3, connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM catalog_offer_products op JOIN furniture f ON f.id = op.furniture_id WHERE f.item_name LIKE 'wf_%group%'"));

            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            Assert.Equal(listed, Listing(connection));
            Assert.Equal(3, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture WHERE item_name LIKE 'wf_%group%'"));
            Assert.Equal(4, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_offers"));
        });
    }

    [RoomComponentDatabaseFact]
    public void AHotelWithoutWiredPagesGetsTheRowsButNoOffers()
    {
        WithCatalogue(false, connection =>
        {
            Assert.Equal(3, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture WHERE item_name LIKE 'wf_%group%'"));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_offers"));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_page_offers"));
        });
    }

    private static (int Page, int Position, string Key, int Credits)[] Listing(MySqlConnection connection) => connection.Query<(int, int, string, int)>(
        "SELECT po.page_id, po.position, o.localization_key, o.cost_credits FROM catalog_page_offers po JOIN catalog_offers o ON o.id = po.offer_id " +
        "WHERE o.localization_key <> 'old' ORDER BY po.page_id, po.position").ToArray();

    private static void WithCatalogue(bool wiredPages, Action<MySqlConnection> body)
    {
        var server = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = "wired_group_boxes_" + Guid.NewGuid().ToString("N");

        using (var setup = new MySqlConnection(server)) {
            setup.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server) { Database = schema }.ConnectionString);
            connection.Open();
            var fresh = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));
            connection.Execute("SET FOREIGN_KEY_CHECKS = 0");

            foreach (var table in new[] { "furniture", "catalog_pages", "catalog_offers", "catalog_offer_products", "catalog_page_offers" }) {
                connection.Execute(WiredMigrationSchema.Table(fresh, table));
            }

            connection.Execute("SET FOREIGN_KEY_CHECKS = 1");
            connection.Execute("""
                INSERT INTO furniture (id, item_name, public_name, sprite_id, interaction_type, has_furnidata)
                    VALUES (10, 'wf_test_26secret3', 'wf_test_26secret3 name', 19512, 'default', 1);
                INSERT INTO catalog_offers (id, localization_key, cost_credits) VALUES (20, 'old', 3);
                INSERT INTO catalog_offer_products (offer_id, position, product_type, furniture_id) VALUES (20, 0, 'furni', 10);
                """);

            if (wiredPages) {
                connection.Execute("""
                    INSERT INTO catalog_pages (id, caption, link, catalog_mode) VALUES
                        (1, 'Wired Effects', 'wired_effects', 'NORMAL'), (2, 'Wired Effects', 'wired_effects', 'BUILDERS_CLUB'),
                        (3, 'Wired Triggers', 'wired_triggers', 'NORMAL'), (4, 'Other', 'other', 'NORMAL');
                    INSERT INTO catalog_page_offers (page_id, offer_id, position) VALUES (1, 20, 4);
                    """);
            }
            else {
                connection.Execute("""
                    INSERT INTO catalog_pages (id, caption, link, catalog_mode) VALUES (4, 'Other', 'other', 'NORMAL');
                    INSERT INTO catalog_page_offers (page_id, offer_id, position) VALUES (4, 20, 0);
                    """);
            }

            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));
            body(connection);
        }
        finally {
            using var cleanup = new MySqlConnection(server);
            cleanup.Execute($"DROP DATABASE `{schema}`");
        }
    }
}

internal static class WiredMigrationSchema
{
    // The table as the fresh install creates it, so the test cannot drift from the shipped schema.
    public static string Table(string dump, string table)
    {
        var start = dump.IndexOf($"CREATE TABLE `{table}` (", StringComparison.Ordinal);

        if (start < 0) {
            throw new InvalidOperationException($"The fresh install has no table {table}.");
        }

        var end = dump.IndexOf(';', dump.IndexOf("ENGINE=", start, StringComparison.Ordinal));

        return dump[start..(end + 1)];
    }
}
