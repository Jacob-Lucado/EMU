using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.LandingView;

internal sealed class GetBonusRareInfoEvent(ISettingsManager settings, IItemDataManager items) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet) => throw new NotImplementedException();
}
