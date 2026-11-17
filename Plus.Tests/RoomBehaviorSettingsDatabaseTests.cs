using Dapper;
using MySqlConnector;
using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

public sealed class RoomBehaviorSettingsDatabaseTests
{
    private const string MigrationFile = "Database/Migrations/137_RoomBehaviorSettings.sql";
    private const string Columns = "leave_on_door_tile, idle_sleep_enabled, idle_sleep_timeout_seconds, idle_autokick_enabled, " +
                                   "idle_autokick_timeout_seconds, mute_all_pets";

    [Fact]
    public void TheFreshInstallRoomsTableEndsWithTheBehaviorSettings()
    {
        var sql = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql")).Replace("\r\n", "\n");
        var table = sql[sql.IndexOf("CREATE TABLE `rooms` (", StringComparison.Ordinal)..];

        Assert.Contains("""
              `diagonal_squeeze` tinyint(1) NOT NULL DEFAULT 0,
              `leave_on_door_tile` tinyint(1) NOT NULL DEFAULT 1,
              `idle_sleep_enabled` tinyint(1) NOT NULL DEFAULT 1,
              `idle_sleep_timeout_seconds` int(11) NOT NULL DEFAULT 300,
              `idle_autokick_enabled` tinyint(1) NOT NULL DEFAULT 1,
              `idle_autokick_timeout_seconds` int(11) NOT NULL DEFAULT 900,
              `mute_all_pets` tinyint(1) NOT NULL DEFAULT 0,
              PRIMARY KEY (`id`),
            """, table[..table.IndexOf(") ENGINE", StringComparison.Ordinal)]);
    }

    [Fact]
    public void TheRoomQueryReadsEveryBehaviorColumn()
    {
        foreach (var (column, property) in new[] { ("leave_on_door_tile", "LeaveOnDoorTile"), ("idle_sleep_enabled", "IdleSleepEnabled"),
                     ("idle_sleep_timeout_seconds", "IdleSleepTimeoutSeconds"), ("idle_autokick_enabled", "IdleAutokickEnabled"),
                     ("idle_autokick_timeout_seconds", "IdleAutokickTimeoutSeconds"), ("mute_all_pets", "MuteAllPets") }) {
            Assert.Contains($"rooms.`{column}` AS {property}", RoomDataMapping.SelectRoom);
        }
    }

    [Fact]
    public void ALoadedRoomKeepsItsStoredBehaviorSettingsAndCopiesThem()
    {
        var model = new RoomModel("model_a", 0, 0, 0, 0, "00\r00", 0, 0, false);
        var data = RoomDataMapping.CreateData(new RoomDataRow
        {
            Id = 40,
            Caption = "room",
            ModelName = "model_a",
            Username = "owner",
            Owner = 7,
            Roomtype = "private",
            State = "open",
            LeaveOnDoorTile = false,
            IdleSleepEnabled = false,
            IdleSleepTimeoutSeconds = 45,
            IdleAutokickEnabled = true,
            IdleAutokickTimeoutSeconds = 120,
            MuteAllPets = true
        }, model, false);

        var expected = new RoomBehaviorSettings(false, false, 45, true, 120, true);
        Assert.Equal(expected, data.Behavior);
        Assert.Equal(expected, new RoomData(data).Behavior);
    }

    [Fact]
    public void ARoomWithoutStoredSettingsUsesTheHabboDefaults()
    {
        Assert.Equal(new RoomBehaviorSettings(true, true, 300, true, 900, false), RoomBehaviorSettings.Default);
        var model = new RoomModel("model_a", 0, 0, 0, 0, "00\r00", 0, 0, false);
        var row = new RoomDataRow { Id = 40, Caption = "room", ModelName = "model_a", Username = "owner", Owner = 7, Roomtype = "private", State = "open" };
        Assert.Equal(RoomBehaviorSettings.Default, RoomDataMapping.CreateData(row, model, false).Behavior);
    }

    // Existing rooms get the Habbo defaults, stored values survive a rerun, and new rooms get the defaults too.
    [RoomComponentDatabaseFact]
    public void TheMigrationGivesEveryRoomTheDefaultsAndCanRunTwice()
    {
        var root = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        using var server = new MySqlConnection(root);
        var schema = "room_behavior_" + Guid.NewGuid().ToString("N");
        server.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(root) { Database = schema }.ConnectionString);
            connection.Execute("CREATE TABLE rooms(id INT PRIMARY KEY); INSERT INTO rooms VALUES (40),(41)");
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile));

            connection.Execute(migration);
            Assert.Equal(["1|1|300|1|900|0", "1|1|300|1|900|0"], Rows(connection));
            connection.Execute("UPDATE rooms SET leave_on_door_tile=0, idle_sleep_timeout_seconds=45, mute_all_pets=1 WHERE id=40");
            connection.Execute(migration);
            Assert.Equal(["0|1|45|1|900|1", "1|1|300|1|900|0"], Rows(connection));
            connection.Execute("INSERT INTO rooms(id) VALUES (42)");
            Assert.Equal("1|1|300|1|900|0", Rows(connection)[2]);
        }
        finally {
            server.Execute($"DROP DATABASE `{schema}`");
        }
    }

    private static string[] Rows(MySqlConnection connection) =>
        connection.Query<string>($"SELECT CONCAT_WS('|', {Columns}) FROM rooms ORDER BY id").ToArray();
}
