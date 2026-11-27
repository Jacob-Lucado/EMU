using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Rooms.Music;

public interface IRoomStreamModeration
{
    bool IsBanned(RoomStream stream);

    bool Ban(RoomStream stream, string bannedBy, string reason);

    bool Unban(RoomStream stream);

    IReadOnlyList<RoomStreamBan> Bans(int count);

    bool IsRestricted(int userId);

    StrikeResult Strike(int userId);

    int Strikes(int userId);

    bool Lift(int userId);

    int? UserId(string username);
}

public sealed record RoomStreamBan(string Url, string Reason, string BannedBy);

public sealed record StrikeResult(int Strikes, bool Restricted);

// Banned links cannot be played, and a user who keeps playing them loses :music until a moderator lifts it.
public sealed class RoomStreamModeration(IDatabase database, ILogger<RoomStreamModeration> logger) : IRoomStreamModeration
{
    public const int StrikeLimit = 3;

    public bool IsBanned(RoomStream stream)
    {
        var keys = stream.BanKeys;

        if (keys.Count == 0) {
            return false;
        }

        try {
            using var connection = database.Connection();

            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM room_stream_bans WHERE ban_key IN @keys", new { keys }) > 0;
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not check the banned songs.");

            return false;
        }
    }

    // The playlist is banned when the link has one, otherwise the video or track.
    public bool Ban(RoomStream stream, string bannedBy, string reason)
    {
        var keys = stream.BanKeys;

        if (keys.Count == 0) {
            return false;
        }

        using var connection = database.Connection();

        return connection.Execute("INSERT IGNORE INTO room_stream_bans (ban_key, url, reason, banned_by) VALUES (@key, @url, @reason, @bannedBy)",
            new { key = keys[0], url = Cut(stream.Url, 500), reason = Cut(reason, 200), bannedBy = Cut(bannedBy, 50) }) > 0;
    }

    public bool Unban(RoomStream stream)
    {
        var keys = stream.BanKeys;

        if (keys.Count == 0) {
            return false;
        }

        using var connection = database.Connection();

        return connection.Execute("DELETE FROM room_stream_bans WHERE ban_key IN @keys", new { keys }) > 0;
    }

    public IReadOnlyList<RoomStreamBan> Bans(int count)
    {
        try {
            using var connection = database.Connection();

            return connection.Query<RoomStreamBan>("SELECT url AS Url, reason AS Reason, banned_by AS BannedBy FROM room_stream_bans ORDER BY id DESC LIMIT @count", new { count }).ToList();
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not read the banned songs.");

            return [];
        }
    }

    public bool IsRestricted(int userId)
    {
        try {
            using var connection = database.Connection();

            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM room_stream_strikes WHERE user_id = @userId AND restricted = 1", new { userId }) > 0;
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not check a music restriction.");

            return false;
        }
    }

    public StrikeResult Strike(int userId)
    {
        using var connection = database.Connection();

        connection.Execute(
            "INSERT INTO room_stream_strikes (user_id, strikes, restricted) VALUES (@userId, 1, @restricted) ON DUPLICATE KEY UPDATE strikes = strikes + 1, restricted = IF(strikes >= @limit, 1, restricted)",
            new { userId, restricted = StrikeLimit <= 1 ? 1 : 0, limit = StrikeLimit });

        var row = connection.QuerySingle<(int Strikes, bool Restricted)>("SELECT strikes, restricted FROM room_stream_strikes WHERE user_id = @userId", new { userId });

        return new(row.Strikes, row.Restricted);
    }

    public int Strikes(int userId)
    {
        using var connection = database.Connection();

        return connection.ExecuteScalar<int?>("SELECT strikes FROM room_stream_strikes WHERE user_id = @userId", new { userId }) ?? 0;
    }

    // Lifting clears the strikes too, so the user starts over.
    public bool Lift(int userId)
    {
        using var connection = database.Connection();

        return connection.Execute("DELETE FROM room_stream_strikes WHERE user_id = @userId", new { userId }) > 0;
    }

    public int? UserId(string username)
    {
        using var connection = database.Connection();

        return connection.ExecuteScalar<int?>("SELECT id FROM users WHERE username = @username LIMIT 1", new { username });
    }

    private static string Cut(string value, int length) => value.Length <= length ? value : value[..length];
}
