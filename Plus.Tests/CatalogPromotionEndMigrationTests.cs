using System.Text.RegularExpressions;
using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

public sealed class CatalogPromotionEndMigrationTests
{
    private const string MigrationFile = "Database/Migrations/132_StockPromotionsUntil2030.sql";
    private const string EndOf2030 = "2030-12-31 23:59:59.000000";

    [Fact]
    public void TheFreshInstallShowsEveryStockPromotionUntilTheEndOf2030()
    {
        var sql = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql")).Replace("\r\n", "\n");
        var start = sql.IndexOf("INSERT INTO `catalog_promotions` VALUES\n", StringComparison.Ordinal);
        var rows = sql[start..sql.IndexOf(";\n", start, StringComparison.Ordinal)];
        var ends = Regex.Matches(rows, @",(NULL|'[^']*')\)").Select(match => match.Groups[1].Value).ToList();

        Assert.Equal(["NULL", $"'{EndOf2030}'", $"'{EndOf2030}'", $"'{EndOf2030}'"], ends);
    }

    // A promotion the hotel added or pointed at another page keeps its end date.
    [RoomComponentDatabaseFact]
    public void TheMigrationExtendsOnlyTheStockPromotionsAndCanRunTwice()
    {
        var root = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        using var server = new MySqlConnection(root);
        var schema = "promotion_end_" + Guid.NewGuid().ToString("N");
        server.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(root) { Database = schema }.ConnectionString);
            connection.Open();
            connection.Execute("""
                CREATE TABLE catalog_promotions (id INT PRIMARY KEY, page_link VARCHAR(128) DEFAULT '', expires_at DATETIME(6) DEFAULT NULL);
                INSERT INTO catalog_promotions VALUES
                    (5, 'ny2015_permeffects', NULL),
                    (6, 'house20bundd', '2026-10-12 10:58:57'),
                    (7, 'hween26furni', '2026-11-02 10:58:57'),
                    (8, 'own_page', '2026-11-02 10:58:57'),
                    (9, 'ducket_furni', '2026-01-01 00:00:00');
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile));

            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(["5|", $"6|{EndOf2030}", $"7|{EndOf2030}", "8|2026-11-02 10:58:57.000000", "9|2026-01-01 00:00:00.000000"],
                connection.Query<string>("SELECT CONCAT_WS('|', id, IFNULL(expires_at, '')) FROM catalog_promotions ORDER BY id"));
        }
        finally {
            server.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
