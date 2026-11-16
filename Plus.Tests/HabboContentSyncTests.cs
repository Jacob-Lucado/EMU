using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

// Habbo's new furniture (Christmas 2026, NFT 2026 and others) exists with Habbo's furnidata, and the clothes it unlocks exist in
// the figure data the server checks figures against.
public sealed partial class HabboContentSyncTests
{
    private const string MigrationFile = "Database/Migrations/139_HabboContentSync.sql";
    private const string StagingTable = "habbo_content_139";
    private const int NewFurniture = 123;

    private static readonly Lazy<string> Migration = new(() => File.Exists(HabbiconPacketTests.Repo(MigrationFile)) ? File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile)) : "");
    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Migrated = new(() => MigrationRows(Migration.Value));
    private static readonly Lazy<Dictionary<string, List<Dictionary<string, string>>>> Fresh = new(() => FreshInstallRows(File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"))));

    [Theory]
    [InlineData("xmas_r26_ferriswheel", "s", "19499", "1", "3", "1", "0", "default", "3", "xmas2026", "76125", "")]
    [InlineData("xmas_c26_giftduck", "s", "19438", "1", "1", "1", "0", "default", "1", "xmas2026", "76088", "")]
    [InlineData("trophy_c26_duckbergine", "s", "19413", "1", "1", "1.2", "0", "trophy", "1", "trophies", "76001", "")]
    [InlineData("clothing_nft26_snowcoat", "s", "19471", "1", "1", "1", "0", "purchasable_clothing", "1", "nft2026", "76113", "6653,6654,6655")]
    [InlineData("nft_h26_candycaneheart", "i", "4938", "1", "1", "0", "1", "default", "1", "nft2026", "76088", "")]
    public void NewFurnitureHasHabbosDefinition(string name, string type, string sprite, string width, string length, string height, string stack,
        string interaction, string modes, string line, string revision, string customParams)
    {
        var expected = new Dictionary<string, string>
        {
            ["type"] = type,
            ["sprite_id"] = sprite,
            ["width"] = width,
            ["length"] = length,
            ["stack_height"] = height,
            ["can_stack"] = stack,
            ["interaction_type"] = interaction,
            ["interaction_modes_count"] = modes,
            ["furni_line"] = line,
            ["revision"] = revision,
            ["has_furnidata"] = "1",
            ["custom_params"] = customParams,
        };

        Assert.True(Migrated.Value.TryGetValue(name, out var migrated), name + " is not in migration 139");
        var fresh = Assert.Single(Fresh.Value.TryGetValue(name, out var rows) ? rows : []);

        foreach (var (column, value) in expected) {
            Assert.True(Same(migrated[column], value), $"migration: {name}.{column} is {migrated[column]}, Habbo has {value}");
            Assert.True(Same(fresh[column], value), $"FreshInstall: {name}.{column} is {fresh[column]}, Habbo has {value}");
        }
    }

    [Fact]
    public void TheMigrationAddsEveryNewClassnameOnceAndTheFreshInstallHasTheSameRows()
    {
        Assert.Equal(NewFurniture, Migrated.Value.Count);

        foreach (var (name, migrated) in Migrated.Value) {
            var fresh = Assert.Single(Fresh.Value.TryGetValue(name, out var rows) ? rows : []);

            foreach (var (column, value) in migrated) {
                Assert.True(Same(fresh[column], value), $"{name}.{column}: FreshInstall {fresh[column]}, migration {value}");
            }
        }
    }

    [Fact]
    public void EveryClothingSetTheNewFurnitureUnlocksIsInTheServersFigureData()
    {
        var sets = XDocument.Load(HabbiconPacketTests.Repo("Config/figuredata.xml")).Descendants("set").Select(set => (string)set.Attribute("id")!).ToHashSet();
        var clothing = Migrated.Value.Values.Where(row => row["interaction_type"] == "purchasable_clothing").ToList();

        Assert.NotEmpty(clothing);
        Assert.All(clothing, row => Assert.All(row["custom_params"].Split(','), id => Assert.Contains(id, sets)));
    }

    [RoomComponentDatabaseFact]
    public void TheMigrationAddsTheRowsOnceAndKeepsAHotelsOwnRows()
    {
        var builder = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")) { AllowUserVariables = true };
        using var connection = new MySqlConnection(builder.ConnectionString);
        connection.Open();
        var schema = "habbo_content_sync_" + Guid.NewGuid().ToString("N");
        connection.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try {
            connection.Execute($"USE `{schema}`");
            connection.Execute(FurnitureTable());
            // A hotel that already made xmas_c26_giftduck itself, and one custom furni on the ferris wheel's sprite id.
            connection.Execute("""
                INSERT INTO furniture (id, item_name, public_name, type, sprite_id, interaction_type, has_furnidata) VALUES
                    (7, 'xmas_c26_giftduck', 'Our duck', 's', 99001, 'default', 1),
                    (8, 'custom_wheel', 'Custom wheel', 's', 19499, 'default', 1);
                """);

            foreach (var statement in Plus.Database.Migrator.SqlScript.Split(Migration.Value)) {
                connection.Execute(statement);
            }

            var afterFirst = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture");

            foreach (var statement in Plus.Database.Migrator.SqlScript.Split(Migration.Value)) {
                connection.Execute(statement);
            }

            Assert.Equal(afterFirst, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture"));
            Assert.Equal(2 + NewFurniture - 2, afterFirst);
            Assert.Equal("Our duck", connection.ExecuteScalar<string>("SELECT public_name FROM furniture WHERE item_name = 'xmas_c26_giftduck'"));
            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM furniture WHERE item_name = 'xmas_r26_ferriswheel'"));
            Assert.Equal("6653,6654,6655", connection.ExecuteScalar<string>("SELECT custom_params FROM furniture WHERE item_name = 'clothing_nft26_snowcoat'"));
            Assert.Equal("trophy", connection.ExecuteScalar<string>("SELECT interaction_type FROM furniture WHERE item_name = 'trophy_c26_duckbergine'"));
            Assert.Equal(4938, connection.ExecuteScalar<int>("SELECT furnidata_sprite_id FROM furniture WHERE item_name = 'nft_h26_candycaneheart' AND type = 'i'"));
        }
        finally {
            connection.Execute($"DROP DATABASE IF EXISTS `{schema}`");
        }
    }

    private static string FurnitureTable()
    {
        var sql = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));
        var start = sql.IndexOf("CREATE TABLE `furniture`", StringComparison.Ordinal);

        return sql[start..(sql.IndexOf(';', start) + 1)];
    }

    private static bool Same(string actual, string expected) =>
        double.TryParse(actual, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var a) &&
        double.TryParse(expected, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var e)
            ? Math.Abs(a - e) < 1e-9
            : string.Equals(actual, expected, StringComparison.Ordinal);

    private static Dictionary<string, Dictionary<string, string>> MigrationRows(string sql)
    {
        var rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var header = StagingInsert().Match(sql);

        if (!header.Success) {
            return rows;
        }

        var columns = Regex.Matches(header.Groups["columns"].Value, "`([a-z_]+)`").Select(m => m.Groups[1].Value).ToList();

        foreach (var line in sql[(header.Index + header.Length)..].Split('\n')) {
            var trimmed = line.Trim();

            if (!trimmed.StartsWith('(')) {
                break;
            }

            var row = columns.Zip(Values(trimmed.TrimEnd(';', ',')[1..^1])).ToDictionary(pair => pair.First, pair => pair.Second);
            Assert.True(rows.TryAdd(row["item_name"], row), "twice in the migration: " + row["item_name"]);
        }

        return rows;
    }

    private static Dictionary<string, List<Dictionary<string, string>>> FreshInstallRows(string sql)
    {
        var table = sql[sql.IndexOf("CREATE TABLE `furniture`", StringComparison.Ordinal)..];
        var columns = Regex.Matches(table[..table.IndexOf("PRIMARY KEY", StringComparison.Ordinal)], @"^\s+`([a-z_]+)`", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToList();
        var rows = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.Ordinal);
        var inFurniture = false;

        foreach (var line in sql.Split('\n')) {
            if (line.StartsWith("INSERT INTO ", StringComparison.Ordinal)) {
                inFurniture = line.StartsWith("INSERT INTO `furniture` VALUES", StringComparison.Ordinal);
            }
            else if (inFurniture && line.StartsWith('(')) {
                var row = columns.Zip(Values(line.TrimEnd('\r', ';', ',')[1..^1])).ToDictionary(pair => pair.First, pair => pair.Second);
                rows.TryAdd(row["item_name"], []);
                rows[row["item_name"]].Add(row);
            }
            else if (!line.StartsWith('(')) {
                inFurniture = false;
            }
        }

        return rows;
    }

    // SQL literals of one tuple; NULL becomes an empty string.
    private static List<string> Values(string tuple)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        var wasQuoted = false;

        for (var i = 0; i < tuple.Length; i++) {
            var c = tuple[i];

            if (quoted && c == '\\' && i + 1 < tuple.Length) {
                current.Append(tuple[++i]);
            }
            else if (c == '\'') {
                quoted = !quoted;
                wasQuoted = true;
            }
            else if (c == ',' && !quoted) {
                values.Add(!wasQuoted && current.ToString().Trim() == "NULL" ? "" : current.ToString().Trim());
                current.Clear();
                wasQuoted = false;
            }
            else {
                current.Append(c);
            }
        }

        values.Add(!wasQuoted && current.ToString().Trim() == "NULL" ? "" : current.ToString().Trim());

        return values;
    }

    [GeneratedRegex(@"INSERT INTO `" + StagingTable + @"` \((?<columns>[^)]*)\) VALUES\s*\n")]
    private static partial Regex StagingInsert();
}
