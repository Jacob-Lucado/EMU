using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.LandingView;

namespace Plus.Communication.Packets.Outgoing.LandingView;

public sealed class BonusRareInfoComposer(BonusRareInfo info) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.BonusRareInfoComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteString(info.ProductType);
        packet.WriteInteger(info.ProductClassId);
        packet.WriteInteger(info.TotalCredits);
        packet.WriteInteger(info.CreditsStillRequired);
    }
}
