using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetOffersEvent(IMarketplaceOfferSearchService search) : IPacketEvent
{
    private const int CollectiblesOfferType = 1;

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var minCost = packet.ReadInt();
        var maxCost = packet.ReadInt();
        var searchQuery = packet.ReadString();
        var filterMode = packet.ReadInt();

        // Every marketplace offer is plain furni, so the collectibles offer type never has offers.
        if (ReadOfferType(packet) == CollectiblesOfferType) {
            session.Send(new MarketPlaceOffersComposer(new MarketplaceOffersSnapshot([])));

            return Task.CompletedTask;
        }

        session.Send(new MarketPlaceOffersComposer(search.Search(minCost, maxCost, searchQuery, filterMode)));

        return Task.CompletedTask;
    }

    private static int ReadOfferType(IIncomingPacket packet)
    {
        if (packet.HasDataRemaining()) {
            packet.ReadBool();
        }

        return packet.HasDataRemaining() ? packet.ReadInt() : 0;
    }
}
