using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Inventory.Pets;

public sealed class PetAddedToInventoryComposer(PetInventoryEntry pet) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetAddedToInventoryComposer;

    public void Compose(IOutgoingPacket packet)
    {
        PetInventoryComposer.WritePet(packet, pet);
        packet.WriteBoolean(true);
    }
}
