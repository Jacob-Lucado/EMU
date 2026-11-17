using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Inventory.Furni;

public sealed class FurniListRemoveMultipleComposer(IReadOnlyList<uint> ids) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.FurniListRemoveMultipleComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(ids.Count);

        foreach (var id in ids) {
            packet.WriteUInteger(id);
        }
    }
}
