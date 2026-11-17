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
    /// <summary>
    /// Pages through the top entries the snapshot keeps. The viewer's row always follows, with rank -1
    /// and score 0 when the viewer is not on the board.
    /// </summary>
    public static BadgeLeaderboardPage From(BadgeLeaderboardSnapshot snapshot, BadgeLeaderboardRequest request, int viewerId, LeaderboardProfile viewer)
    {
        var board = (BadgeLeaderboardType)request.Type switch
        {
            BadgeLeaderboardType.BadgesByRarity => snapshot.RarityBoards.GetValueOrDefault((BadgeRarityTier)request.Rarity),
            BadgeLeaderboardType.AchievementLevel => snapshot.AchievementLevel,
            _ => snapshot.TotalBadges
        };
        var top = board?.Top.ToImmutableArray() ?? [];
        var start = (long)request.Page * request.Size;
        var entries = request.Page < 0 || request.Size <= 0 || start >= top.Length
            ? []
            : top.Skip((int)start).Take(request.Size).Select((score, index) => Entry(snapshot, score, (int)start + index + 1)).ToImmutableArray();
        var own = board?.Find(viewerId);

        return new(request, top.Length, entries, new(viewerId, viewer.Username, viewer.Figure, own?.Rank ?? -1, own?.Score ?? 0));
    }

    private static BadgeLeaderboardEntry Entry(BadgeLeaderboardSnapshot snapshot, LeaderboardScore score, int rank)
    {
        var profile = snapshot.Profiles.GetValueOrDefault(score.UserId);

        return new(score.UserId, profile?.Username ?? "", profile?.Figure ?? "", rank, score.Score);
    }
}
