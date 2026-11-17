using System.Collections.Immutable;

namespace Plus.HabboHotel.Badges.Rarity;

public enum BadgeLeaderboardType
{
    TotalBadges = 0,
    BadgesByRarity = 1,
    AchievementLevel = 2
}

public sealed record BadgeLeaderboardRequest(int Type, int Rarity, int Page, int Size);

public sealed record BadgeLeaderboardEntry(int UserId, string Username, string Figure, int Rank, int Score);

/// <summary>One chunk of a badge leaderboard and the viewer's own row.</summary>
public sealed record BadgeLeaderboardPage(BadgeLeaderboardRequest Request, int TotalEntries, ImmutableArray<BadgeLeaderboardEntry> Entries,
    BadgeLeaderboardEntry Own)
{
    public static BadgeLeaderboardPage From(BadgeLeaderboardSnapshot snapshot, BadgeLeaderboardRequest request, int viewerId, LeaderboardProfile viewer) =>
        throw new NotImplementedException();
}
