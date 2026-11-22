namespace Plus.HabboHotel.Rooms.Music;

// A web audio link a room plays for everyone in it. Kind is "stream" for a direct audio stream, "soundcloud" for a SoundCloud link and "youtube" for a YouTube video.
public sealed record RoomStream(string Kind, string Url, string SetBy)
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
            var videoId = Chat.Commands.User.TvCommand.VideoIdOf(link);

            return videoId == null ? null : new("youtube", $"https://www.youtube.com/watch?v={videoId}", setBy);
        }

        var kind = Array.IndexOf(SoundCloudHosts, uri.Host.ToLowerInvariant()) >= 0 ? "soundcloud" : "stream";

        return new(kind, uri.AbsoluteUri, setBy);
    }
}
