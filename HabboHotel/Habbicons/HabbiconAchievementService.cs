using System.Data.Common;
using Microsoft.Extensions.Logging;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Habbicons;

[Singleton]
public interface IHabbiconAchievementService
{
    void Reconcile(GameClient session, HabbiconSnapshot snapshot);
    void Used(GameClient session);
}

public sealed class HabbiconAchievementService(IAchievementManager achievements,
    ILogger<HabbiconAchievementService>? logger = null) : IHabbiconAchievementService
{
    // A set counts only when every member is owned, although a claimable member already completes it for its reward.
    public void Reconcile(GameClient session, HabbiconSnapshot snapshot)
    {
        Reconcile(session, "ACH_HabbiconCollected", snapshot.Items.Values.Count(item => item.Owned));
        Reconcile(session, "ACH_HabbiconCollectionComp",
            snapshot.Collections.Count(collection => collection.Items.Count > 0 && collection.Items.All(item => item.Owned)));
    }

    public void Used(GameClient session) => Progress(session, "ACH_HabbiconUsed", 1);

    // An unchanged total is skipped, unless it already reaches a level that is still missing after a failed level-up.
    private void Reconcile(GameClient session, string group, int total)
    {
        var stored = session.GetHabbo()?.GetAchievementData(group);

        if ((stored?.Progress ?? 0) == total &&
            !(achievements.Achievements.GetValueOrDefault(group)?.NextLevel(stored?.Level ?? 0) is { } next && next.Requirement <= total)) {
            return;
        }

        Progress(session, group, total, absolute: true);
    }

    private void Progress(GameClient session, string group, int amount, bool absolute = false)
    {
        try {
            achievements.ProgressAchievement(session, group, amount, fromBeginning: absolute);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception)) {
            logger?.LogError(exception, "Unable to update Habbicon achievement {Group} for user {UserId}", group, session.GetHabbo().Id);
        }
    }

    // The badge store is awaited with .Result, which wraps its database error in an AggregateException.
    private static bool IsDatabaseFailure(Exception exception) => exception is DbException ||
        exception is AggregateException aggregate && aggregate.Flatten().InnerExceptions.All(inner => inner is DbException);
}
