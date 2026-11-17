using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.LandingView;

namespace Plus.Communication.Packets.Outgoing.LandingView;

public sealed class BonusRareInfoComposer(BonusRareInfo info) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.BonusRareInfoComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
