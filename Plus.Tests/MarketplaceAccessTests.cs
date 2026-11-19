using Plus.Communication.Packets.Incoming.Marketplace;
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

        await new GetMarketplaceCanMakeOfferEvent(locks).Parse(client, null!);

        Assert.Equal(new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }, Assert.Single(sent).Payload);
    }

    [Fact]
    public async Task ATradingLockedUserGetsTheTradingLockDenial()
    {
        var locks = CatalogSnapshotTestSupport.Proxy<ITradingLockService>((_, _) => true);
        var (client, sent) = HabbiconTestSupport.Client(new Habbo());

        await new GetMarketplaceCanMakeOfferEvent(locks).Parse(client, null!);

        Assert.Equal(new byte[] { 0, 0, 0, 6, 0, 0, 0, 0 }, Assert.Single(sent).Payload);
    }
}
