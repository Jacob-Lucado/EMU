using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class PetLevelNotificationComposer(int petId, string name, int level, string look) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetLevelNotificationComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(petId);
        packet.WriteString(name);
        packet.WriteInteger(level);
        PetFigureData.Write(packet, look);
    }
}
