using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Televisions;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.YouTubeTelevisions;

internal class YouTubeGetNextVideo(ITelevisionPresentationService televisions) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadInt();
        var command = packet.ReadInt();

        // 0 is previous and 1 is next. Pause and continue (2 and 3) must not change the video.
        if (command is 0 or 1) {
            televisions.ShowNextVideo(session, itemId);
        }

        return Task.CompletedTask;
    }
}
