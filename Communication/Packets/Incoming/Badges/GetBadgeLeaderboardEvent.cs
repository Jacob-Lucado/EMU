using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Badges;

internal sealed class GetBadgeLeaderboardEvent(IBadgeRarityManager rarity) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet) => throw new NotImplementedException();
}
