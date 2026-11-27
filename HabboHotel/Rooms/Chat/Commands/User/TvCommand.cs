using System.Text.RegularExpressions;
using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Televisions;
using Plus.HabboHotel.Items.VideoScreens;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal partial class TvCommand : IChatCommand
{
    private readonly IDatabase _database;
    private readonly ITelevisionManager _televisions;

    private readonly IVideoScreens? _videoScreens;

    public TvCommand(IDatabase database, ITelevisionManager televisions, IVideoScreens? videoScreens = null)
    {
        _database = database;
        _televisions = televisions;
        _videoScreens = videoScreens;
    }

    public string Key => "tv";

    public string Parameters => "%youtube link% | music | off";

    public string Description => "Play a YouTube video on every TV and Video Screen in the room and add it to the playlist. On Video Screens, :tv music shows the room music and :tv off clears them.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the TV.");

            return;
        }

        var items = room.GetRoomItemHandler().GetWallAndFloor.ToList();
        var videoScreens = _videoScreens == null ? [] : items.Where(item => item.Definition.ItemName == VideoScreens.ItemName).ToList();
        var word = parameters.Length > 0 ? parameters[0].Trim().ToLowerInvariant() : "";

        // A Video Screen can also follow the room music or be cleared.
        if (word is "music" or "off") {
            if (videoScreens.Count == 0) {
                session.SendWhisper("There is no Video Screen in this room.");

                return;
            }

            var mode = word == "music" ? VideoScreens.FollowMusic : "";

            foreach (var screen in videoScreens) {
                _videoScreens!.Set(screen.Id, mode);
                room.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, mode));
            }

            session.SendWhisper(word == "music" ? "Done! The Video Screens in this room show the room music." : "Done! The Video Screens in this room are off.");

            return;
        }

        var videoId = VideoIdFrom(parameters);

        if (videoId == null) {
            session.SendWhisper("Paste a YouTube link, like :tv https://youtu.be/dQw4w9WgXcQ");

            return;
        }

        var screens = items.Where(item => item.Definition.InteractionType == InteractionType.Television).ToList();

        if (screens.Count == 0 && videoScreens.Count == 0) {
            session.SendWhisper("There is no TV or Video Screen in this room.");

            return;
        }

        AddToPlaylist(_database, videoId, session.GetHabbo().Username);

        _televisions.Init();

        // A TV shows its video when someone clicks it; anyone already watching switches now.
        foreach (var screen in screens) {
            _televisions.NowShowing[(int)screen.Id] = videoId;
            room.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, videoId));
        }

        foreach (var screen in videoScreens) {
            _videoScreens!.Set(screen.Id, videoId);
            room.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, videoId));
        }

        var parts = new List<string>();

        if (screens.Count > 0) {
            parts.Add($"{screens.Count} TV{(screens.Count == 1 ? "" : "s")} (click one to watch)");
        }

        if (videoScreens.Count > 0) {
            parts.Add($"{videoScreens.Count} Video Screen{(videoScreens.Count == 1 ? "" : "s")}");
        }

        session.SendWhisper($"Done! It's set on {string.Join(" and ", parts)} in this room.");
    }

    // The video joins the TV playlist, so it shows up when someone opens a screen.
    internal static void AddToPlaylist(IDatabase database, string videoId, string username)
    {
        var title = $"Added by {username}";

        if (title.Length > 50) {
            title = title[..50];
        }

        using var connection = database.Connection();
        connection.Execute("INSERT INTO items_youtube (youtube_id, title, description, enabled) SELECT @videoId, @title, @description, 1 FROM DUAL " +
            "WHERE NOT EXISTS (SELECT 1 FROM items_youtube WHERE youtube_id = @videoId)",
            new { videoId, title, description = $"https://youtu.be/{videoId}" });
    }

    // The command word is already gone when the command runs: the link is the first parameter.
    internal static string? VideoIdFrom(string[] parameters) => parameters.Length > 0 ? VideoIdOf(parameters[0]) : null;

    // A YouTube video ID is 11 letters, digits, '-' or '_'.
    internal static string? VideoIdOf(string text)
    {
        var match = VideoId().Match(text.Trim());

        return match.Success ? match.Groups["id"].Value : null;
    }

    [GeneratedRegex(@"^(?:https?://)?(?:(?:www\.|m\.|music\.)?youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/|live/)|youtu\.be/)?(?<id>[A-Za-z0-9_-]{11})(?:[?&#/].*)?$")]
    private static partial Regex VideoId();
}
