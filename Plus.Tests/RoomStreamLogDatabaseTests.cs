using System.Data;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Rooms.Music;
using Xunit;

namespace Plus.Tests;

public sealed class RoomStreamLogDatabaseFactAttribute : FactAttribute
{
    public const string Variable = "ROOM_STREAM_LOG_DATABASE";

    public RoomStreamLogDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable))) {
            Skip = $"Set {Variable} to a MariaDB server connection string (no Database) that may create scratch schemas.";
        }
    }
}

public sealed class RoomStreamLogDatabaseTests : IDisposable
{
    private readonly MySqlConnectionStringBuilder _server;
    private readonly string _schema = "room_stream_log_tests_" + Guid.NewGuid().ToString("N")[..12];
    private readonly bool _enabled = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RoomStreamLogDatabaseFactAttribute.Variable));

    public RoomStreamLogDatabaseTests()
    {
        _server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable(RoomStreamLogDatabaseFactAttribute.Variable) ?? "Server=localhost");

        if (_enabled) {
            using var server = new MySqlConnection(_server.ConnectionString);
            server.Execute($"CREATE DATABASE `{_schema}` CHARACTER SET utf8mb4");
        }
    }

    public void Dispose()
    {
        if (_enabled) {
            using var server = new MySqlConnection(_server.ConnectionString);
            server.Execute($"DROP DATABASE IF EXISTS `{_schema}`");
        }
    }

    [RoomStreamLogDatabaseFact]
    public void RecordsPlaysAndRanksThisWeeksTopSongs()
    {
        var database = new ScratchDatabase(new MySqlConnectionStringBuilder(_server.ConnectionString) { Database = _schema }.ConnectionString);
        using (var connection = database.Connection()) {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Database", "Migrations", "177_RoomStreamPlays.sql"))) {
                directory = directory.Parent;
            }

            var script = File.ReadAllText(Path.Combine(directory!.FullName, "Database", "Migrations", "177_RoomStreamPlays.sql"));
            connection.Execute(script);
            connection.Execute(script);
        }

        var log = new RoomStreamLog(database, NullLogger<RoomStreamLog>.Instance);
        log.Record(1, new RoomStream("youtube", "https://youtu.be/a", "Alice"));
        log.Record(2, new RoomStream("youtube", "https://youtu.be/a", "Bob"));
        log.Record(2, new RoomStream("audio", "https://example.com/b.mp3", "Bob"));
        log.Record(3, new RoomStream("audio", "https://example.com/" + new string('x', 600), new string('n', 80)));

        using (var connection = database.Connection()) {
            connection.Execute("INSERT INTO room_stream_plays (room_id, set_by, kind, url, played_at) VALUES (1, 'Old', 'youtube', 'https://youtu.be/old', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 9 DAY))");
            Assert.Equal(500, connection.ExecuteScalar<int>("SELECT MAX(CHAR_LENGTH(url)) FROM room_stream_plays"));
            Assert.Equal(50, connection.ExecuteScalar<int>("SELECT MAX(CHAR_LENGTH(set_by)) FROM room_stream_plays"));
        }

        var top = log.Top(2);

        Assert.Equal(2, top.Count);
        Assert.Equal(new TopSong("https://youtu.be/a", 2), top[0]);
        Assert.DoesNotContain(log.Top(10), song => song.Url.EndsWith("old"));
    }

    [Fact]
    public void ALogFailureNeverThrows()
    {
        var log = new RoomStreamLog(new ScratchDatabase("Server=127.0.0.1;Port=1;User=x;ConnectionTimeout=1"), NullLogger<RoomStreamLog>.Instance);

        log.Record(1, new RoomStream("youtube", "https://youtu.be/a", "Alice"));

        Assert.Empty(log.Top(5));
    }

    private sealed class ScratchDatabase(string connectionString) : IDatabase
    {
        public bool IsConnected() => true;

        public IDbConnection Connection()
        {
            var connection = new MySqlConnection(connectionString);
            connection.Open();

            return connection;
        }
    }
}
