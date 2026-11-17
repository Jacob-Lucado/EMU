using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Badges;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public class BadgeLeaderboardPacketTests
{
    private const int Viewer = 5;

    [Fact]
    public async Task FirstChunkListsTheTopOfTheTotalBadgesBoardAndTheViewer()
    {
        // Users 1..60 own 61 - id badges, so user 1 ranks first. The viewer (5) ranks fifth.
        var snapshot = Board(60);

        var packet = await Request(snapshot, 0, -1, 0, 50);

        Assert.Equal(new[] { 0, -1, 0, 50, 60, 50 }, Ints(packet, 6));

        for (var i = 1; i <= 50; i++) {
            AssertEntry(packet, i, "user" + i, "look" + i, i, 61 - i);
        }

        Assert.True(packet.ReadBool());
        AssertEntry(packet, Viewer, "johndoe", "hd-180-1", 5, 56);
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public async Task LaterChunkStartsAfterTheEarlierOnes()
    {
        var packet = await Request(Board(60), 0, -1, 1, 50);

        Assert.Equal(new[] { 0, -1, 1, 50, 60, 10 }, Ints(packet, 6));

        for (var i = 51; i <= 60; i++) {
            AssertEntry(packet, i, "user" + i, "look" + i, i, 61 - i);
        }

        Assert.True(packet.ReadBool());
        AssertEntry(packet, Viewer, "johndoe", "hd-180-1", 5, 56);
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public async Task BoardListsAtMostTheTopEntries()
    {
        var packet = await Request(Board(BadgeLeaderboardSnapshot.EntryLimit + 20), 0, -1, 2, 50);

        Assert.Equal(new[] { 0, -1, 2, 50, BadgeLeaderboardSnapshot.EntryLimit, 0 }, Ints(packet, 6));
    }

    [Fact]
    public async Task ViewerOffTheBoardGetsAnUnrankedOwnRow()
    {
        var snapshot = BadgeLeaderboardSnapshot.Build([new(1, "A")], [], BadgeRarityScale.Empty)
            .WithProfiles(new Dictionary<int, LeaderboardProfile> { [1] = new("user1", "look1") });

        var packet = await Request(snapshot, 0, -1, 0, 50);

        Assert.Equal(new[] { 0, -1, 0, 50, 1, 1 }, Ints(packet, 6));
        AssertEntry(packet, 1, "user1", "look1", 1, 1);
        Assert.True(packet.ReadBool());
        AssertEntry(packet, Viewer, "johndoe", "hd-180-1", -1, 0);
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public async Task AchievementLevelBoardRanksByAchievementScore()
    {
        var snapshot = BadgeLeaderboardSnapshot.Build([], [new(1, 300), new(Viewer, 900)], BadgeRarityScale.Empty)
            .WithProfiles(new Dictionary<int, LeaderboardProfile> { [1] = new("user1", "look1"), [Viewer] = new("old", "old") });

        var packet = await Request(snapshot, 2, -1, 0, 50);

        Assert.Equal(new[] { 2, -1, 0, 50, 2, 2 }, Ints(packet, 6));
        AssertEntry(packet, Viewer, "old", "old", 1, 900);
        AssertEntry(packet, 1, "user1", "look1", 2, 300);
        Assert.True(packet.ReadBool());
        AssertEntry(packet, Viewer, "johndoe", "hd-180-1", 1, 900);
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public async Task RarityBoardCountsTheBadgesOfThatRarity()
    {
        // A badge with one owner is unique; badge P has two owners and is legendary on an empty scale.
        var snapshot = BadgeLeaderboardSnapshot.Build([new(1, "U"), new(1, "P"), new(Viewer, "P")], [], BadgeRarityScale.Empty)
            .WithProfiles(new Dictionary<int, LeaderboardProfile> { [1] = new("user1", "look1") });

        var unique = await Request(snapshot, 1, (int)BadgeRarityTier.Unique, 0, 50);

        Assert.Equal(new[] { 1, (int)BadgeRarityTier.Unique, 0, 50, 1, 1 }, Ints(unique, 6));
        AssertEntry(unique, 1, "user1", "look1", 1, 1);
        Assert.True(unique.ReadBool());
        AssertEntry(unique, Viewer, "johndoe", "hd-180-1", -1, 0);
        Assert.False(unique.HasDataRemaining());

        var missing = await Request(snapshot, 1, 42, 0, 50);

        Assert.Equal(new[] { 1, 42, 0, 50, 0, 0 }, Ints(missing, 6));
        Assert.True(missing.ReadBool());
        AssertEntry(missing, Viewer, "johndoe", "hd-180-1", -1, 0);
        Assert.False(missing.HasDataRemaining());
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(int.MaxValue, 50)]
    public async Task ImpossibleChunkHasNoEntries(int page, int size)
    {
        var packet = await Request(Board(60), 0, -1, page, size);

        Assert.Equal(new[] { 0, -1, page, size, 60, 0 }, Ints(packet, 6));
    }

    private static BadgeLeaderboardSnapshot Board(int users)
    {
        var ownership = new List<BadgeOwnership>();
        var profiles = new Dictionary<int, LeaderboardProfile>();

        for (var id = 1; id <= users; id++) {
            for (var badge = 0; badge < users + 1 - id; badge++) {
                ownership.Add(new(id, "B" + badge));
            }

            profiles[id] = new("user" + id, "look" + id);
        }

        return BadgeLeaderboardSnapshot.Build(ownership, [], BadgeRarityScale.Empty).WithProfiles(profiles);
    }

    private static async Task<FlashIncomingPacket> Request(BadgeLeaderboardSnapshot snapshot, int type, int rarity, int page, int size)
    {
        var (client, sent) = HabbiconTestSupport.Client(new Habbo { Id = Viewer, Username = "johndoe", Look = "hd-180-1" });

        await new GetBadgeLeaderboardEvent(new TestBadgeRarity(snapshot)).Parse(client, HabbiconTestSupport.Incoming(type, rarity, page, size));

        var (header, payload) = Assert.Single(sent);
        Assert.Equal(ServerPacketHeader.BadgeLeaderboardComposer, header);

        return new FlashIncomingPacket { Buffer = payload };
    }

    private static int[] Ints(FlashIncomingPacket packet, int count) => Enumerable.Range(0, count).Select(_ => packet.ReadInt()).ToArray();

    private static void AssertEntry(FlashIncomingPacket packet, int userId, string name, string figure, int rank, int score)
    {
        Assert.Equal(userId, packet.ReadInt());
        Assert.Equal(name, packet.ReadString());
        Assert.Equal(figure, packet.ReadString());
        Assert.Equal(rank, packet.ReadInt());
        Assert.Equal(score, packet.ReadInt());
    }
}
