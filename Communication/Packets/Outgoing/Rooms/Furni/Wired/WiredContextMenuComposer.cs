using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired.Modern.Addons;

namespace Plus.Communication.Packets.Outgoing.Rooms.Furni.Wired;

public class WiredContextMenuComposer(int targetType, int targetId, IReadOnlyList<WiredContextMenuEntry> items) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.WiredContextMenuComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(targetType);
        packet.WriteInteger(targetId);
        packet.WriteInteger(items.Count);

        foreach (var item in items) {
            packet.WriteString(item.ButtonId);
            packet.WriteString(item.Text);
            packet.WriteBoolean(item.Enabled);
        }
    }
}
