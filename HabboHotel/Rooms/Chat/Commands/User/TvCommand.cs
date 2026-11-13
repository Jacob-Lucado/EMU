using System.Text.RegularExpressions;
using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Televisions;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal partial class TvCommand : IChatCommand
{
    private readonly IDatabase _database;
    private readonly ITelevisionManager _televisions;

    public TvCommand(IDatabase database, ITelevisionManager televisions)
    {
        _database = database;
        _televisions = televisions;
    }

    public string Key => "tv";

    public string Parameters => "%youtube link%";

    public string Description => "Play a YouTube video on every TV in the room.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the TV.");

            return;
        }

        var videoId = parameters.Length > 1 ? VideoIdOf(parameters[1]) : null;

        if (videoId == null) {
            session.SendWhisper("Paste a YouTube link, like :tv https://youtu.be/dQw4w9WgXcQ");

            return;
        }

        var screens = room.GetRoomItemHandler().GetWallAndFloor.Where(item => item.Definition.InteractionType == InteractionType.Television).ToList();

        if (screens.Count == 0) {
            session.SendWhisper("There is no TV in this room.");

            return;
        }

        var title = $"Added by {session.GetHabbo().Username}";

        if (title.Length > 50) {
            title = title[..50];
        }

        // The video also joins the TV playlist, so it shows up when someone opens a screen.
        using (var connection = _database.Connection()) {
            connection.Execute("INSERT INTO items_youtube (youtube_id, title, description, enabled) SELECT @videoId, @title, @description, 1 FROM DUAL " +
                "WHERE NOT EXISTS (SELECT 1 FROM items_youtube WHERE youtube_id = @videoId)",
                new { videoId, title, description = $"https://youtu.be/{videoId}" });
        }

        _televisions.Init();

        // A TV shows its video when someone clicks it; anyone already watching switches now.
        foreach (var screen in screens) {
            _televisions.NowShowing[(int)screen.Id] = videoId;
            room.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, videoId));
        }

        session.SendWhisper($"Done! Click a TV to watch. It's set on {screens.Count} TV{(screens.Count == 1 ? "" : "s")} in this room.");
    }

    // A YouTube video ID is 11 letters, digits, '-' or '_'.
    internal static string? VideoIdOf(string text)
    {
        var match = VideoId().Match(text.Trim());

        return match.Success ? match.Groups["id"].Value : null;
    }

    [GeneratedRegex(@"^(?:https?://)?(?:(?:www\.|m\.|music\.)?youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/|live/)|youtu\.be/)?(?<id>[A-Za-z0-9_-]{11})(?:[?&#/].*)?$")]
    private static partial Regex VideoId();
}
