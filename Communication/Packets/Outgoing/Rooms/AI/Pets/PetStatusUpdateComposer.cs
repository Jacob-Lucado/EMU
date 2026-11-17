using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class PetStatusUpdateComposer(int virtualId, int petId, bool hasBreedingPermission) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetStatusUpdateComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
