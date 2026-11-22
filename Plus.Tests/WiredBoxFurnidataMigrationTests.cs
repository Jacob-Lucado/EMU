using System.Text.Json.Nodes;
using Dapper;
using MySqlConnector;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Items;
using Xunit;

namespace Plus.Tests;

/// <summary>Migration 174: the group and context menu wired boxes get their own sprite ids and furnidata.</summary>
public sealed class WiredBoxFurnidataMigrationTests
{
    private const string Migration = "Database/Migrations/174_WiredBoxFurnidata.sql";

    private static readonly (string Box, int Sprite, string Name, string Placeholder, int PlaceholderSprite)[] Boxes =
    [
        ("wf_act_invite_to_group", 9000000, "WIRED Effect: Invite to Group", "wf_test_26secret3", 19512),
        ("wf_act_remove_from_group", 9000001, "WIRED Effect: Remove from Group", "wf_test_26secret4", 19513),
        ("wf_trg_group_membership", 9000002, "WIRED Trigger: Group Membership Changed", "wf_test_26secret5", 19514),
        ("wf_trg_click_context_menu", 9000003, "WIRED Trigger: User Clicks Context Menu", "wf_test_26secret6", 19515),
        ("wf_xtra_context_menu_item", 9000004, "WIRED Add-on: Context Menu Item", "wf_test_26secret7", 19516)
    ];

    [RoomComponentDatabaseFact]
    public void EachBoxGetsItsOwnSpriteAndFurnidataAndThePlaceholdersStay()
    {
        WithHotel(withBoxes: true, connection =>
        {
            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            Assert.Equal(Boxes.Select(box => (box.Box, box.Sprite, 1, box.Name)).OrderBy(row => row.Box), BoxRows(connection));
            Assert.Equal(Boxes.Select(box => (box.Placeholder, box.PlaceholderSprite, 1, box.Placeholder + " name")).OrderBy(row => row.Placeholder),
                PlaceholderRows(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void TheGeneratedFurnidataNamesEachBoxAndDrawsItsPlaceholder()
    {
        WithHotel(withBoxes: true, connection =>
        {
            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            var file = CatalogFurnidata.Generate(new FurnidataRepository(connection).All(), []);
            var entries = JsonNode.Parse(file.Content)!["roomitemtypes"]!["furnitype"]!.AsArray().OfType<JsonObject>()
                .ToDictionary(entry => (int)entry["id"]!);

            foreach (var box in Boxes) {
                Assert.Equal(box.Placeholder, (string)entries[box.Sprite]["classname"]!);
                Assert.Equal(box.Name, (string)entries[box.Sprite]["name"]!);
                Assert.Equal(box.Placeholder, (string)entries[box.PlaceholderSprite]["classname"]!);
                Assert.Equal(box.Placeholder + " name", (string)entries[box.PlaceholderSprite]["name"]!);
            }
        });
    }

    [RoomComponentDatabaseFact]
    public void AListedBoxOnTheMarketplaceShowsTheBoxSprite()
    {
        WithHotel(withBoxes: true, connection =>
        {
            connection.Execute("""
                SET FOREIGN_KEY_CHECKS = 0;
                INSERT INTO catalog_marketplace_offers (offer_id, item_id, user_id, asking_price, public_name, sprite_id, extra_data, furni_id)
                    SELECT 1, id, 7, 10, item_name, sprite_id, '', 500 FROM furniture WHERE item_name = 'wf_act_invite_to_group';
                INSERT INTO catalog_marketplace_offers (offer_id, item_id, user_id, asking_price, public_name, sprite_id, extra_data, furni_id)
                    SELECT 2, id, 7, 10, item_name, sprite_id, '', 501 FROM furniture WHERE item_name = 'wf_test_26secret3';
                SET FOREIGN_KEY_CHECKS = 1;
                """);

            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            Assert.Equal([(1, 9000000), (2, 19512)],
                connection.Query<(int, int)>("SELECT offer_id, sprite_id FROM catalog_marketplace_offers ORDER BY offer_id"));
        });
    }

    [RoomComponentDatabaseFact]
    public void CustomSpritesRemainExclusiveWhenBoxesAreMigrated()
    {
        WithHotel(withBoxes: true, connection =>
        {
            connection.Execute("""
                INSERT INTO furniture (item_name, public_name, sprite_id, interaction_type, has_furnidata)
                    VALUES ('custom_chair', 'Custom chair', 9000002, 'default', 1),
                           ('custom_alias', 'Custom alias', 9000003, 'default', 0)
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));
            connection.Execute(migration);
            var rows = BoxRows(connection);
            Assert.Equal(7, rows.Select(row => row.Item2).Distinct().Count());
            Assert.Contains(rows, row => row.Item1 == "custom_chair" && row.Item2 == 9000002);
            connection.Execute(migration);
            Assert.Equal(rows, BoxRows(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void ARerunChangesNothing()
    {
        WithHotel(withBoxes: true, connection =>
        {
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));
            connection.Execute(migration);
            var boxes = BoxRows(connection);
            var placeholders = PlaceholderRows(connection);

            connection.Execute(migration);

            Assert.Equal(boxes, BoxRows(connection));
            Assert.Equal(placeholders, PlaceholderRows(connection));
            Assert.Equal(10, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture"));
        });
    }

    [RoomComponentDatabaseFact]
    public void AHotelWithoutTheBoxesChangesNothing()
    {
        WithHotel(withBoxes: false, connection =>
        {
            var placeholders = PlaceholderRows(connection);

            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            Assert.Empty(BoxRows(connection));
            Assert.Equal(placeholders, PlaceholderRows(connection));
        });
    }

    [Fact]
    public void TheFreshInstallAlreadyHasEachBoxWithItsOwnSpriteAndFurnidata()
    {
        var furniture = File.ReadAllLines(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));

        foreach (var box in Boxes) {
            var row = furniture.Single(line => line.StartsWith('(') && line.Contains($",'{box.Box}','", StringComparison.Ordinal));
            var fields = Fields(row);

            Assert.Equal(box.Sprite.ToString(), fields[10]);
            Assert.Equal("1", fields[25]);
            Assert.Equal(box.Sprite.ToString(), fields[^2]);
            Assert.Equal($"'{box.Box}'", fields[^1]);
        }
    }

    // The values of one dump row such as (1,'a',2), with quoted strings kept whole.
    private static List<string> Fields(string row)
    {
        var body = row.TrimEnd(',', ';')[1..^1];
        var fields = new List<string>();
        var start = 0;
        var quoted = false;

        for (var index = 0; index < body.Length; index++) {
            if (body[index] == '\\') {
                index++;
            }
            else if (body[index] == '\'') {
                quoted = !quoted;
            }
            else if (body[index] == ',' && !quoted) {
                fields.Add(body[start..index]);
                start = index + 1;
            }
        }

        fields.Add(body[start..]);

        return fields;
    }

    private static (string, int, int, string)[] BoxRows(MySqlConnection connection) => connection.Query<(string, int, int, string)>(
        "SELECT item_name, sprite_id, has_furnidata, name FROM furniture WHERE item_name NOT LIKE 'wf_test_%' ORDER BY item_name").ToArray();

    private static (string, int, int, string)[] PlaceholderRows(MySqlConnection connection) => connection.Query<(string, int, int, string)>(
        "SELECT item_name, sprite_id, has_furnidata, name FROM furniture WHERE item_name LIKE 'wf_test_%' ORDER BY item_name").ToArray();

    // A hotel with the five placeholder furni of Habbo, then migrations 172 and 173 when withBoxes.
    private static void WithHotel(bool withBoxes, Action<MySqlConnection> body)
    {
        var server = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = "wired_box_furnidata_" + Guid.NewGuid().ToString("N");

        using (var setup = new MySqlConnection(server)) {
            setup.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server) { Database = schema }.ConnectionString);
            connection.Open();
            var fresh = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));
            connection.Execute("SET FOREIGN_KEY_CHECKS = 0");

            foreach (var table in new[] { "furniture", "catalog_pages", "catalog_offers", "catalog_offer_products", "catalog_page_offers", "catalog_marketplace_offers" }) {
                connection.Execute(WiredMigrationSchema.Table(fresh, table));
            }

            connection.Execute("SET FOREIGN_KEY_CHECKS = 1");

            foreach (var box in Boxes) {
                connection.Execute("""
                    INSERT INTO furniture (item_name, public_name, sprite_id, interaction_type, has_furnidata, name)
                        VALUES (@Placeholder, CONCAT(@Placeholder, ' name'), @PlaceholderSprite, 'default', 1, CONCAT(@Placeholder, ' name'))
                    """, new { box.Placeholder, box.PlaceholderSprite });
            }

            if (withBoxes) {
                connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo("Database/Migrations/172_WiredGroupBoxes.sql")));
                connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo("Database/Migrations/173_WiredContextMenuBoxes.sql")));
            }

            body(connection);
        }
        finally {
            using var cleanup = new MySqlConnection(server);
            cleanup.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
