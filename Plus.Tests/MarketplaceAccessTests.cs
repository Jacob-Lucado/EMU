using Plus.Communication.Packets.Incoming.Marketplace;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.Talents;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public sealed class MarketplaceAccessTests
{
    [Fact]
    public async Task AUserWithoutTradingPassOrCitizenshipMayMakeAnOffer()
    {
        var locks = CatalogSnapshotTestSupport.Proxy<ITradingLockService>((_, _) => false);
        var (client, sent) = HabbiconTestSupport.Client(new Habbo());

        await new GetMarketplaceCanMakeOfferEvent(locks, NewUserAccess()).Parse(client, null!);

        Assert.Equal(new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }, Assert.Single(sent).Payload);
    }

    [Fact]
    public async Task ATradingLockedUserGetsTheTradingLockDenial()
    {
        var locks = CatalogSnapshotTestSupport.Proxy<ITradingLockService>((_, _) => true);
        var (client, sent) = HabbiconTestSupport.Client(new Habbo());

        await new GetMarketplaceCanMakeOfferEvent(locks, NewUserAccess()).Parse(client, null!);

        Assert.Equal(new byte[] { 0, 0, 0, 6, 0, 0, 0, 0 }, Assert.Single(sent).Payload);
    }

    // A citizenship track whose TRADE level the new user has not completed.
    internal static MarketplaceAccessPolicy NewUserAccess()
    {
        var definitions = new Dictionary<string, Achievement>
        {
            ["ACH_RegistrationDuration"] = new() { Id = 42, GroupName = "ACH_RegistrationDuration", Category = "identity", Levels = new() { [1] = new(1, 0, 0, 1) } }
        };
        var levels = new List<TalentTrackLevel> { new("citizenship", 2, "TRADE", "", [new(1, "ACH_RegistrationDuration1", 1)]) };

        return new MarketplaceAccessPolicy(CatalogSnapshotTestSupport.Proxy<ITalentTrackManager>((_, _) => levels),
            CatalogSnapshotTestSupport.Proxy<IAchievementManager>((_, _) => definitions));
    }
}
