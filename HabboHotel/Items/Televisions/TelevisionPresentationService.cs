using System.Collections.Immutable;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.VideoScreens;

namespace Plus.HabboHotel.Items.Televisions;

public interface ITelevisionPresentationService
{
    void ShowPlaylist(GameClient session, int itemId);
    void ShowNextVideo(GameClient session, int itemId);
    void ShowVideoInformation(GameClient session, int itemId, string videoId);
}

public sealed class TelevisionPresentationService(ITelevisionManager televisions, IVideoScreens? videoScreens = null) : ITelevisionPresentationService
{
    public void ShowVideoInformation(GameClient session, int itemId, string videoId)
    {
        if (VideoScreenOf(session, itemId) is { } screen) {
            if (televisions.TelevisionList.Any(video => video.YouTubeId == videoId)) {
                ShowOnScreen(session, screen, videoId);
            }

            return;
        }

        var matches = televisions.TelevisionList.Where(video => video.YouTubeId == videoId)
            .Select(video => video.YouTubeId).ToImmutableArray();

        foreach (var match in matches) {
            session.Send(new GetYouTubeVideoComposer(itemId, match));
        }
    }

    public void ShowNextVideo(GameClient session, int itemId)
    {
        if (!session.GetHabbo().InRoom) {
            return;
        }

        if (televisions.TelevisionList.Count == 0) {
            session.SendNotification("Oh, it looks like the hotel manager haven't added any videos for you to watch! :(");

            return;
        }

        var candidates = televisions.Televisions.Values.ToList();
        var next = candidates[Random.Shared.Next(candidates.Count)].YouTubeId;

        if (VideoScreenOf(session, itemId) is { } screen) {
            ShowOnScreen(session, screen, next);

            return;
        }

        session.Send(new GetYouTubeVideoComposer(itemId, next));
    }

    public void ShowPlaylist(GameClient session, int itemId)
    {
        if (!session.GetHabbo().InRoom) {
            return;
        }

        var playlist = televisions.TelevisionList.Select(TelevisionVideoSnapshot.Capture).ToImmutableArray();

        if (playlist.IsEmpty) {
            session.SendNotification("Oh, it looks like the hotel manager haven't added any videos for you to watch! :(");

            return;
        }

        // The TV's own video is shown first, or a random one, then the whole playlist, as the client expects.
        var candidates = televisions.Televisions.Values.ToList();
        var screen = VideoScreenOf(session, itemId);

        // A Video Screen reports what it shows: its video, "@music" or nothing.
        var first = screen != null ? videoScreens!.Get(screen.Id)
            : televisions.NowShowing.TryGetValue(itemId, out var chosen) ? chosen : candidates[Random.Shared.Next(candidates.Count)].YouTubeId;
        session.Send(new GetYouTubeVideoComposer(screen != null ? (int)screen.Id : itemId, first));
        session.Send(new GetYouTubePlaylistComposer(itemId, playlist));
    }

    public Item? VideoScreenOf(GameClient session, int itemId) =>
        videoScreens != null && session.GetHabbo().CurrentRoom?.GetRoomItemHandler().GetItem((uint)BuildersClubItemIds.FromWire(itemId)) is { } item
        && item.Definition.ItemName == Plus.HabboHotel.Items.VideoScreens.VideoScreens.ItemName
            ? item
            : null;

    // Choosing a video on a Video Screen changes it for everyone in the room.
    public void ShowOnScreen(GameClient session, Item screen, string videoId)
    {
        videoScreens!.Set(screen.Id, videoId);
        session.GetHabbo().CurrentRoom?.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, videoId));
    }
}
