using System.Text.RegularExpressions;
using System.Web;

namespace Plus.HabboHotel.Rooms.Music;

// A web audio link a room plays for everyone in it. Kind is "stream" for a direct audio stream, "soundcloud" for a SoundCloud link or playlist and "youtube" for a YouTube video or playlist.
public sealed partial record RoomStream(string Kind, string Url, string SetBy)
{
    private const int MaxLength = 500;

    private static readonly string[] SoundCloudHosts = ["soundcloud.com", "www.soundcloud.com", "m.soundcloud.com", "on.soundcloud.com", "api.soundcloud.com"];

    private static readonly string[] YouTubeHosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be"];

    // Only secure links are accepted, because the client page is secure and the browser blocks plain http audio.
    public static RoomStream? Parse(string text, string setBy)
    {
        var link = text.Trim();

        if (link.Length == 0 || link.Length > MaxLength || !Uri.TryCreate(link, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0 || uri.Host.Length == 0) {
            return null;
        }

        if (Array.IndexOf(YouTubeHosts, uri.Host.ToLowerInvariant()) >= 0) {
            return ParseYouTube(link, uri, setBy);
        }

        var kind = Array.IndexOf(SoundCloudHosts, uri.Host.ToLowerInvariant()) >= 0 ? "soundcloud" : "stream";

        return new(kind, uri.AbsoluteUri, setBy);
    }

    // The keys a ban can match: the playlist when the link has one, and the video.
    public IReadOnlyList<string> BanKeys
    {
        get
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri)) {
                return [];
            }

            switch (Kind) {
                case "youtube": {
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    var keys = new List<string>();

                    if (query["list"] is { Length: > 0 } list) {
                        keys.Add($"youtube-list:{list}");
                    }

                    if (query["v"] is { Length: > 0 } video) {
                        keys.Add($"youtube:{video}");
                    }

                    return keys;
                }
                case "soundcloud":
                    return [$"soundcloud:{uri.Host.ToLowerInvariant().Replace("www.", "").Replace("m.", "")}{uri.AbsolutePath.TrimEnd('/').ToLowerInvariant()}"];
                default:
                    return [$"stream:{uri.Host.ToLowerInvariant()}{uri.AbsolutePath.ToLowerInvariant()}"];
            }
        }
    }

    // A YouTube link can name a video, a playlist, or a video inside a playlist.
    private static RoomStream? ParseYouTube(string link, Uri uri, string setBy)
    {
        var list = HttpUtility.ParseQueryString(uri.Query)["list"];
        var hasList = list != null && PlaylistId().IsMatch(list);

        if (hasList && uri.AbsolutePath.TrimEnd('/').Equals("/playlist", StringComparison.OrdinalIgnoreCase)) {
            return new("youtube", $"https://www.youtube.com/playlist?list={list}", setBy);
        }

        var videoId = Chat.Commands.User.TvCommand.VideoIdOf(link);

        if (videoId == null) {
            return null;
        }

        return new("youtube", hasList ? $"https://www.youtube.com/watch?v={videoId}&list={list}" : $"https://www.youtube.com/watch?v={videoId}", setBy);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{10,64}$")]
    private static partial Regex PlaylistId();
}
