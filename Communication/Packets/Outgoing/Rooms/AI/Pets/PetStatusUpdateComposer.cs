using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class PetStatusUpdateComposer(int virtualId, int petId, bool canBreed, bool canHarvest, bool canRevive, bool breedingPermission)
    : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetStatusUpdateComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(virtualId);
        packet.WriteInteger(petId);
        packet.WriteBoolean(canBreed);
        packet.WriteBoolean(canHarvest);
        packet.WriteBoolean(canRevive);
        packet.WriteBoolean(breedingPermission);
    }
}
