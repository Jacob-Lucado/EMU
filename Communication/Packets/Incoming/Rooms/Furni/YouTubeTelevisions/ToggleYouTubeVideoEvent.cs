using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.VideoScreens;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.YouTubeTelevisions;

internal class ToggleYouTubeVideoEvent(IVideoScreens? videoScreens = null) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadInt(); //Item Id
        var videoId = packet.ReadString(); //Video ID

        // Picking a video on a Video Screen changes it for everyone in the room.
        if (videoScreens != null && session.GetHabbo().CurrentRoom is { } room
            && room.GetRoomItemHandler().GetItem((uint)BuildersClubItemIds.FromWire(itemId)) is { } screen
            && screen.Definition.ItemName == VideoScreens.ItemName) {
            videoScreens.Set(screen.Id, videoId);
            room.SendPacket(new GetYouTubeVideoComposer((int)screen.Id, videoId));

            return Task.CompletedTask;
        }

        session.Send(new GetYouTubeVideoComposer(itemId, videoId));

        return Task.CompletedTask;
    }
}
