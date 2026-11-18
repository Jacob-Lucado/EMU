using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class PetLevelUpdateComposer(int virtualId, int petId, int level) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetLevelUpdateComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(virtualId);
        packet.WriteInteger(petId);
        packet.WriteInteger(level);
    }
}
