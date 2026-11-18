using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Inventory.Pets;

public sealed class PetRemovedFromInventoryComposer(int petId) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetRemovedFromInventoryComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteInteger(petId);
}
