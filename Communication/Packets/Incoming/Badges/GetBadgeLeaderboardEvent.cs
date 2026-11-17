using Plus.Communication.Packets.Outgoing.Badges;
using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Badges;

/// <summary>Type, rarity, page and page size of the badge leaderboard chunk the client wants.</summary>
internal sealed class GetBadgeLeaderboardEvent(IBadgeRarityManager rarity) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var request = new BadgeLeaderboardRequest(packet.ReadInt(), packet.ReadInt(), packet.ReadInt(), packet.ReadInt());
        var habbo = session.GetHabbo();

        session.Send(new BadgeLeaderboardComposer(BadgeLeaderboardPage.From(rarity.Snapshot, request, habbo.Id, new(habbo.Username, habbo.Look))));

        return Task.CompletedTask;
    }
}
