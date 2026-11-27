using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Items.VideoScreens;

public interface IVideoScreens
{
    // "" is off, "@music" follows the room music, anything else is a YouTube video id.
    string Get(uint itemId);

    void Set(uint itemId, string value);
}

public sealed class VideoScreens(IDatabase database, ILogger<VideoScreens> logger) : IVideoScreens
{
    public const string ItemName = "ads_videoplayer";
    public const string FollowMusic = "@music";

    private readonly ConcurrentDictionary<uint, string> _cache = new();

    public string Get(uint itemId)
    {
        if (_cache.TryGetValue(itemId, out var cached)) {
            return cached;
        }

        var value = FollowMusic;

        try {
            using var connection = database.Connection();
            value = connection.ExecuteScalar<string?>("SELECT value FROM room_video_screens WHERE item_id = @itemId", new { itemId }) ?? FollowMusic;
        } catch (Exception exception) {
            logger.LogWarning(exception, "Could not read a video screen.");
        }

        return _cache[itemId] = value;
    }

    public void Set(uint itemId, string value)
    {
        using var connection = database.Connection();
        connection.Execute("INSERT INTO room_video_screens (item_id, value) VALUES (@itemId, @value) ON DUPLICATE KEY UPDATE value = @value", new { itemId, value });
        _cache[itemId] = value;
    }
}
