using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Xunit;
using static Plus.Tests.HabbiconTestSupport;

namespace Plus.Tests;

public class BadgesRankTests
{
    // User 30 owns three badges, users 10 and 20 two each (the lower id ranks first), user 40 none.
    private static readonly BadgeLeaderboardSnapshot Leaderboard = BadgeLeaderboardSnapshot.Build(
    [
        new(30, "A"), new(30, "B"), new(30, "C"),
        new(20, "A"), new(20, "B"),
        new(10, "A"), new(10, "C")
    ], [], BadgeRarityScale.Empty);

    [Theory]
    [InlineData(30, 1)]
    [InlineData(10, 2)]
    [InlineData(20, 3)]
    [InlineData(40, -1)]
    public void RankIsThePlaceOnTheTotalBadgesBoard(int userId, int rank)
    {
        Assert.Equal(rank, Leaderboard.BadgesRank(userId));
        Assert.Equal(rank, Plus.HabboHotel.Users.ProfileBadgeStats.From(Leaderboard, userId).Rank);
    }

    [Theory]
    [InlineData(20, 3)]
    [InlineData(40, -1)]
    public void UsersEndsWithTheBadgesRankOfTheUser(int userId, int rank)
    {
        var user = Human(userId);
        var clients = CatalogSnapshotTestSupport.Proxy<IGameClientManager>((method, _) =>
            method == "GetClientByUserId" ? user.GetClient() : null);
        var service = new RoomUserSnapshotService(CatalogSnapshotTestSupport.Proxy<IGroupManager>((_, _) => null), clients,
            CatalogSnapshotTestSupport.Proxy<ICacheManager>((_, _) => null), CatalogSnapshotTestSupport.Proxy<IDatabase>((_, _) => null),
            new TestBadgeRarity(Leaderboard));
        var packet = new RecordingPacket();

        new UsersComposer(Assert.IsType<RoomUserSnapshot>(service.Capture(user))).Compose(packet);

        Assert.Equal(rank, packet.Writes[^1]);
    }

    [Theory]
    [InlineData(30, 1)]
    [InlineData(40, -1)]
    public void UserChangeEndsWithTheBadgesRankOfTheUser(int userId, int rank)
    {
        var user = Human(userId);
        var packet = new RecordingPacket();

        new UserChangeComposer(AvatarChangeSnapshot.Capture(user, false, Leaderboard)).Compose(packet);

        Assert.Equal(new object[] { 4, "hd-180-1", "M", "hi", 12, "", 0, rank }, packet.Writes);
    }

    [Fact]
    public void CaptureReadsThePublishedSnapshotByDefault()
    {
        // An id no other test uses, because the published snapshot is shared.
        const int userId = 918_273;
        var user = Human(userId);
        var previous = BadgeLeaderboardSnapshot.Current;

        try {
            BadgeLeaderboardSnapshot.Current = BadgeLeaderboardSnapshot.Build([new(userId, "A")], [], BadgeRarityScale.Empty);

            Assert.Equal(1, AvatarChangeSnapshot.Capture(user, true).BadgesRank);
            Assert.Equal(-1, AvatarChangeSnapshot.Capture(user, true, BadgeLeaderboardSnapshot.Empty).BadgesRank);
        }
        finally {
            BadgeLeaderboardSnapshot.Current = previous;
        }
    }

    private static RoomUser Human(int id)
    {
        var habbo = new Habbo
        {
            Id = id,
            Username = "johndoe",
            Motto = "hi",
            Look = "hd-180-1",
            Gender = "M",
            HabboStats = new HabboStats(0, 0, 0, 0, 0, 0, 12, 0, 0, "")
        };

        return new RoomUser(id, 1, 4, null!, Client(habbo).Client, TestChatEmotions.Unused, TestRewardProgress.Unused);
    }
}
