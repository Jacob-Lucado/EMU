using Plus.Communication.Packets.Outgoing.LandingView;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.LandingView;

namespace Plus.Communication.Packets.Incoming.LandingView;

internal sealed class GetBonusRareInfoEvent(ISettingsManager settings, IItemDataManager items) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.Send(new BonusRareInfoComposer(BonusRareInfo.From(settings, items)));

        return Task.CompletedTask;
    }
}
