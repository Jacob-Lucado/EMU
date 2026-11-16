using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Plus.Tests;

// Furniture definitions follow Habbo's furnidata (size, sit, stand, stack, stack height, lay) and the states its furni assets draw.
public sealed partial class FurnitureHabboParityTests
{
    private const string MigrationFile = "Database/Migrations/138_FurnitureHabboParity.sql";

    private static readonly Lazy<string> Migration = new(() => File.Exists(HabbiconPacketTests.Repo(MigrationFile)) ? File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile)) : "");
    private static readonly Lazy<FreshInstallFurniture> Fresh = new(() => FreshInstallFurniture.Load(File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"))));

    [Theory]
    [InlineData("scifiport*0", "interaction_type", "gate")]
    [InlineData("table_silo_small", "interaction_type", "default")]
    [InlineData("hblooza_icecream", "stack_height", "4")]
    [InlineData("bazaar_c17_curtainpurpleyellowtrim", "interaction_modes_count", "2")]
    [InlineData("SF_alien", "interaction_modes_count", "2")]
    public void EveryRowOfTheClassHasHabbosValue(string name, string column, string value)
    {
        Assert.Contains(Statements(), statement => statement.Column == column && statement.Value == value && statement.Names.Contains(name));
        Assert.All(Fresh.Value.Rows(name), row => Assert.Equal(value, row[column]));
    }

    [Fact]
    public void APlacedItemAboveTheNewLastStateMovesToIt()
    {
        var clamp = Migration.Value.Split(';').Single(statement => statement.Contains("UPDATE items") && statement.Contains("'SF_alien'"));

        Assert.Contains("SET i.extra_data = '1'", clamp);
        Assert.Contains("CAST(i.extra_data AS UNSIGNED) >= 2", clamp);
    }

    [Fact]
    public void ALaserDoorThatWasATrophyStartsClosed()
    {
        var reset = Migration.Value.Split(';').Single(statement => statement.Contains("UPDATE items") && statement.Contains("'scifiport*0'"));

        Assert.Contains("SET i.extra_data = '0'", reset);
    }

    [Fact]
    public void TheFreshInstallAlreadyHasEveryValueTheMigrationSets()
    {
        var statements = Statements();

        Assert.NotEmpty(statements);

        foreach (var statement in statements) {
            foreach (var name in statement.Names) {
                foreach (var row in Fresh.Value.Rows(name).Where(row => statement.Interactions.Count == 0 || statement.Interactions.Contains(row["interaction_type"].ToLowerInvariant()))) {
                    Assert.True(Same(row[statement.Column], statement.Value), $"{name}: {statement.Column} is {row[statement.Column]}, the migration sets {statement.Value}");
                }
            }
        }
    }

    [Fact]
    public void EveryStatementOnlyChangesRowsThatDiffer() =>
        Assert.All(Statements(), statement => Assert.True(statement.Guarded, statement.Column + " = " + statement.Value));

    private static bool Same(string actual, string expected) =>
        double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) && double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var e)
            ? Math.Abs(a - e) < 1e-9
            : string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private sealed record Statement(string Column, string Value, IReadOnlySet<string> Names, IReadOnlySet<string> Interactions, bool Guarded);

    private static List<Statement> Statements()
    {
        var statements = new List<Statement>();

        foreach (Match match in FurnitureUpdate().Matches(Migration.Value)) {
            var column = match.Groups["column"].Value;
            var value = match.Groups["value"].Value.Trim('\'');
            var where = match.Groups["where"].Value;
            var names = Quoted(NameList().Match(where).Groups["names"].Value);
            var interactions = InteractionList().Match(where) is { Success: true } interaction ? Quoted(interaction.Groups["values"].Value) : new HashSet<string>();
            var guarded = where.Contains($"{column} <> {match.Groups["value"].Value}", StringComparison.Ordinal);
            statements.Add(new(column, value, names, interactions, guarded));
        }

        return statements;
    }

    private static HashSet<string> Quoted(string list) => QuotedValue().Matches(list).Select(m => m.Groups[1].Value.Replace("''", "'")).ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"UPDATE furniture SET (?<column>[a-z_]+) = (?<value>'[^']*'|[0-9.]+) WHERE (?<where>[^;]+);")]
    private static partial Regex FurnitureUpdate();

    [GeneratedRegex(@"item_name IN \((?<names>[^)]*)\)")]
    private static partial Regex NameList();

    [GeneratedRegex(@"interaction_type IN \((?<values>[^)]*)\)")]
    private static partial Regex InteractionList();

    [GeneratedRegex(@"'((?:[^']|'')*)'")]
    private static partial Regex QuotedValue();

    /// <summary>The furniture rows of FreshInstall.sql by item_name, with values by column name.</summary>
    private sealed class FreshInstallFurniture
    {
        private readonly Dictionary<string, List<Dictionary<string, string>>> _rows = new(StringComparer.Ordinal);

        public IReadOnlyList<Dictionary<string, string>> Rows(string name) => _rows.TryGetValue(name, out var rows) ? rows : [];

        public static FreshInstallFurniture Load(string sql)
        {
            var table = sql[sql.IndexOf("CREATE TABLE `furniture`", StringComparison.Ordinal)..];
            var columns = Regex.Matches(table[..table.IndexOf("PRIMARY KEY", StringComparison.Ordinal)], @"^\s+`([a-z_]+)`", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToList();
            var fresh = new FreshInstallFurniture();
            var inFurniture = false;

            foreach (var line in sql.Split('\n')) {
                if (line.StartsWith("INSERT INTO ", StringComparison.Ordinal)) {
                    inFurniture = line.StartsWith("INSERT INTO `furniture` VALUES", StringComparison.Ordinal);
                }
                else if (inFurniture && line.StartsWith('(')) {
                    var values = Values(line.TrimEnd('\r', ';', ',')[1..^1]);
                    var row = columns.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second);
                    fresh._rows.TryAdd(row["item_name"], []);
                    fresh._rows[row["item_name"]].Add(row);
                }
            }

            return fresh;
        }

        private static List<string> Values(string tuple)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var quoted = false;

            for (var i = 0; i < tuple.Length; i++) {
                var c = tuple[i];

                if (quoted && c == '\\' && i + 1 < tuple.Length) {
                    current.Append(tuple[++i]);
                }
                else if (c == '\'') {
                    quoted = !quoted;
                }
                else if (c == ',' && !quoted) {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());

            return values;
        }
    }
}
