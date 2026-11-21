using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired.Modern.Addons;

namespace Plus.Communication.Packets.Outgoing.Rooms.Furni.Wired;

public class WiredContextMenuComposer(int targetType, int targetId, IReadOnlyList<WiredContextMenuEntry> items) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.WiredContextMenuComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
