using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Badges.Rarity;

namespace Plus.Communication.Packets.Outgoing.Badges;

public sealed class BadgeLeaderboardComposer(BadgeLeaderboardPage page) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.BadgeLeaderboardComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
