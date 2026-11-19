using System.Data.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Habbicons;
using Plus.HabboHotel.Talents;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory;
using Xunit;

namespace Plus.Tests;

public sealed class HabbiconAchievementTests
{
    private static readonly string[] Groups = ["ACH_HabbiconCollected", "ACH_HabbiconCollectionComp", "ACH_HabbiconUsed"];

    [Fact]
    public void ReconciliationCountsOwnedIconsAndFullyOwnedCollectionsWithoutAddingTotals()
    {
        var calls = new List<(int User, string Group, int Progress, bool Absolute)>();
        var service = new HabbiconAchievementService(Recording(calls));
        var (owner, _) = HabbiconTestSupport.Client(new Habbo { Id = 7 });
        var (other, _) = HabbiconTestSupport.Client(new Habbo { Id = 8 });
        var snapshot = Owned(HabbiconTestSupport.Snapshot());
        service.Reconcile(owner, snapshot);
        service.Reconcile(owner, snapshot);
        service.Reconcile(other, HabbiconTestSupport.Snapshot());
        service.Used(owner);

        Assert.Equal(new[] {
            (7, "ACH_HabbiconCollected", 3, true), (7, "ACH_HabbiconCollectionComp", 1, true),
            (7, "ACH_HabbiconCollected", 3, true), (7, "ACH_HabbiconCollectionComp", 1, true),
            (8, "ACH_HabbiconCollected", 1, true),
            (7, "ACH_HabbiconUsed", 1, false)
        }, calls);
    }

    [Fact]
    public void CompletedCollectionsWithClaimableMembersDoNotCountAsCollectedSets()
    {
        var calls = new List<(int User, string Group, int Progress, bool Absolute)>();
        var (client, _) = HabbiconTestSupport.Client(new Habbo { Id = 7 });
        client.GetHabbo().Achievements["ACH_HabbiconCollectionComp"] = new("ACH_HabbiconCollectionComp", 0, 4);
        var snapshot = Owned(HabbiconTestSupport.Snapshot());
        var claimable = snapshot.Collections[0].Items[1] with { State = HabbiconState.Claimable };
        snapshot = snapshot with { Collections = [snapshot.Collections[0] with { Items = [snapshot.Collections[0].Items[0], claimable] }] };
        new HabbiconAchievementService(Recording(calls)).Reconcile(client, snapshot);

        Assert.True(snapshot.Collections[0].Completed);
        Assert.Contains((7, "ACH_HabbiconCollectionComp", 0, true), calls);
    }

    [Fact]
    public void UnchangedTotalsSendNothingUnlessALevelIsStillDue()
    {
        var calls = new List<(int User, string Group, int Progress, bool Absolute)>();
        var service = new HabbiconAchievementService(Recording(calls));
        var (client, _) = HabbiconTestSupport.Client(new Habbo { Id = 7 });
        var snapshot = Owned(HabbiconTestSupport.Snapshot());
        client.GetHabbo().Achievements["ACH_HabbiconCollected"] = new("ACH_HabbiconCollected", 2, 3);
        client.GetHabbo().Achievements["ACH_HabbiconCollectionComp"] = new("ACH_HabbiconCollectionComp", 1, 1);
        service.Reconcile(client, snapshot);
        Assert.Empty(calls);

        client.GetHabbo().Achievements["ACH_HabbiconCollected"] = new("ACH_HabbiconCollected", 1, 3);
        service.Reconcile(client, snapshot);
        Assert.Equal(new[] { (7, "ACH_HabbiconCollected", 3, true) }, calls);
    }

    [Fact]
    public void BadgeStoreFailuresDuringALevelUpAreContained()
    {
        var (client, _) = HabbiconTestSupport.Client(LevelUpReady());
        var service = new HabbiconAchievementService(FailingBadgeManager(new FailedDatabase()));

        service.Reconcile(client, HabbiconTestSupport.Snapshot());

        Assert.Equal(0, client.GetHabbo().GetAchievementData("ACH_HabbiconCollected")!.Level);
    }

    [Fact]
    public void BadgeFailuresThatAreNotDatabaseErrorsStayVisible()
    {
        var (client, _) = HabbiconTestSupport.Client(LevelUpReady());
        var service = new HabbiconAchievementService(FailingBadgeManager(new InvalidOperationException("bug")));

        Assert.ThrowsAny<Exception>(() => service.Reconcile(client, HabbiconTestSupport.Snapshot()));
    }

    [Theory]
    [InlineData(HabbiconAction.Buy, true)]
    [InlineData(HabbiconAction.BuyCollection, true)]
    [InlineData(HabbiconAction.Claim, true)]
    [InlineData(HabbiconAction.Favorite, false)]
    [InlineData(HabbiconAction.Unfavorite, false)]
    public void OnlySuccessfulAcquisitionsReconcileAchievementsAfterThePurchaseAnswer(HabbiconAction action, bool reconciles)
    {
        var store = new HabbiconTestSupport.Service();
        var calls = new List<HabbiconSnapshot>();
        var (client, sent) = HabbiconTestSupport.Client(new Habbo());
        var achievements = CatalogSnapshotTestSupport.Proxy<IHabbiconAchievementService>((method, args) =>
        {
            Assert.Equal(nameof(IHabbiconAchievementService.Reconcile), method);
            Assert.NotEmpty(store.Actions);
            Assert.Contains(sent, packet => packet.Header == ServerPacketHeader.PurchaseOKComposer);
            calls.Add((HabbiconSnapshot)args![1]!);

            return null;
        });
        var service = new HabbiconPresentationService(store, NullLogger<HabbiconPresentationService>.Instance, achievements);
        service.Change(client, action, 61);
        Assert.Equal(reconciles ? 1 : 0, calls.Count);
        calls.Clear();
        store.Rejection = 4;
        service.Change(client, action, 61);
        Assert.Empty(calls);
    }

    private static HabbiconSnapshot Owned(HabbiconSnapshot snapshot)
    {
        var items = snapshot.Items.ToDictionary();
        items[62] = items[62] with { State = HabbiconState.Owned };
        items[71] = items[71] with { State = HabbiconState.Owned };

        return snapshot with
        {
            Items = items,
            Collections = [snapshot.Collections[0] with { Completed = true, RewardState = HabbiconState.Owned, Items = [items[61], items[62]] }]
        };
    }

    private static IAchievementManager Recording(List<(int User, string Group, int Progress, bool Absolute)> calls)
    {
        var definitions = Definitions();

        return CatalogSnapshotTestSupport.Proxy<IAchievementManager>((method, args) =>
        {
            if (method == "get_" + nameof(IAchievementManager.Achievements)) {
                return definitions;
            }

            Assert.Equal(nameof(IAchievementManager.ProgressAchievement), method);
            calls.Add((((GameClient)args![0]!).GetHabbo().Id, (string)args[1]!, (int)args[2]!, (bool)args[3]!));

            return true;
        });
    }

    private static Dictionary<string, Achievement> Definitions()
    {
        var definitions = new Dictionary<string, Achievement>();

        foreach (var group in Groups) {
            var achievement = new Achievement { GroupName = group, Category = "explore" };
            achievement.AddLevel(new AchievementLevel(1, 1, 2, 1));
            achievement.AddLevel(new AchievementLevel(2, 1, 2, 2));
            achievement.AddLevel(new AchievementLevel(3, 1, 2, 10));
            definitions.Add(group, achievement);
        }

        return definitions;
    }

    private static Habbo LevelUpReady() => new()
    {
        Id = 7,
        HabboStats = new(0, 0, 0, 0, 0, 0, 0, 0, 0, "old"),
        Inventory = new InventoryComponent()
    };

    private static AchievementManager FailingBadgeManager(Exception failure)
    {
        var badges = CatalogSnapshotTestSupport.Proxy<IBadgeManager>((method, _) =>
            method == nameof(IBadgeManager.ReplaceBadge) ? Task.FromException<bool>(failure) : throw new InvalidOperationException(method));
        var talents = CatalogSnapshotTestSupport.Proxy<ITalentTrackProgressionService>((method, _) =>
            method == "Progress" ? null : throw new InvalidOperationException(method));
        var manager = new AchievementManager(null!, null!, badges, talents, null!);

        foreach (var (group, achievement) in Definitions()) {
            manager.Achievements.Add(group, achievement);
        }

        return manager;
    }

    private sealed class FailedDatabase : DbException { }
}
