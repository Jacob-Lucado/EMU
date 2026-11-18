using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class GoToBreedingNestFailureComposer(BreedingNestFailure reason) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.GoToBreedingNestFailureComposer;
    public void Compose(IOutgoingPacket packet) => packet.WriteInteger((int)reason);
}
