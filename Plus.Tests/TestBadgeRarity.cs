using Plus.HabboHotel.Badges.Rarity;

namespace Plus.Tests;

internal sealed class TestBadgeRarity(BadgeLeaderboardSnapshot snapshot) : IBadgeRarityManager
{
    public static TestBadgeRarity Empty { get; } = new(BadgeLeaderboardSnapshot.Empty);

    public BadgeLeaderboardSnapshot Snapshot => snapshot;

    public Task Refresh() => Task.CompletedTask;

    public Task<LeaderboardProfile?> GetProfile(int userId) => Task.FromResult<LeaderboardProfile?>(null);
}
