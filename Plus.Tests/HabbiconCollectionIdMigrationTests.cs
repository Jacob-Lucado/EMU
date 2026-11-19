using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

public sealed class HabbiconCollectionIdMigrationTests
{
    [RoomComponentDatabaseFact]
    public void RenumberingKeepsHotelCollectionsPricesAndOwnedIcons()
    {
        var options = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")) { AllowUserVariables = true };
        var schema = "habbicon_ids_" + Guid.NewGuid().ToString("N");
        using var admin = new MySqlConnection(options.ConnectionString);
        admin.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try {
            options.Database = schema;
            using var connection = new MySqlConnection(options.ConnectionString);
            connection.Open();
            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo("Resources/SQLs/Updates/14_Habbicons.sql")));
            connection.Execute("""
                UPDATE habbicon_collections SET name = 'bread', cost_points = 17 WHERE id = 6;
                INSERT INTO habbicon_collections (id, name) VALUES (1005, 'custom');
                INSERT INTO habbicons (id, collection_id, name) VALUES (901, 1005, 'custom');
                INSERT INTO users_habbicons (user_id, habbicon_id, state) VALUES (1, 61, 3);
                """);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo("Database/Migrations/163_HabbiconCollectionIdsLikeHabbo.sql"));
            connection.Execute(migration);
            connection.Execute(migration);

            Assert.Equal(["duck", "duck2", "frank", "bread", "custom"],
                connection.Query<string>("SELECT name FROM habbicon_collections ORDER BY id"));
            Assert.Equal(17, connection.QuerySingle<int>("SELECT cost_points FROM habbicon_collections WHERE id = 8"));
            Assert.Equal(8, connection.QuerySingle<int>("SELECT h.collection_id FROM users_habbicons u JOIN habbicons h ON h.id = u.habbicon_id WHERE u.state = 3"));
            Assert.Equal(1005, connection.QuerySingle<int>("SELECT collection_id FROM habbicons WHERE id = 901"));
        }
        finally {
            admin.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
