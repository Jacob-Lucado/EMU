using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Rooms.Music;

public interface IRoomStreamLog
{
    void Record(uint roomId, RoomStream stream);

    IReadOnlyList<TopSong> Top(int count);
}

public sealed record TopSong(string Url, int Plays);

public sealed class RoomStreamLog(IDatabase database, ILogger<RoomStreamLog> logger) : IRoomStreamLog
{
    // A failed log entry never stops the music.
    public void Record(uint roomId, RoomStream stream)
    {
        try {
            using var connection = database.Connection();
            connection.Execute("INSERT INTO room_stream_plays (room_id, set_by, kind, url) VALUES (@roomId, @setBy, @kind, @url)",
                new { roomId, setBy = Cut(stream.SetBy, 50), kind = Cut(stream.Kind, 16), url = Cut(stream.Url, 500) });
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not log a room song.");
        }
    }

    public IReadOnlyList<TopSong> Top(int count)
    {
        try {
            using var connection = database.Connection();

            return connection.Query<TopSong>("SELECT url AS Url, COUNT(*) AS Plays FROM room_stream_plays WHERE played_at >= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY) GROUP BY url ORDER BY Plays DESC, url LIMIT @count", new { count }).ToList();
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not read the top songs.");

            return [];
        }
    }

    private static string Cut(string value, int length) => value.Length <= length ? value : value[..length];
}
