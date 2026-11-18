using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Commands.User;
using Plus.HabboHotel.Rooms.Trading;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Clothing.Parts;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

// The wallet in memory changes only after the database step that removes the credit furni and pays its credits.
public sealed class CreditFurniRedeemWalletTests
{
    [Fact]
    public void AFailedTradeRedeemLeavesTheWalletAndSendsNoBalance()
    {
        var (habbo, client, sent) = Owner();
        var store = new FailingTradeStore();

        Assert.ThrowsAny<Exception>(() => Trade.ReceiveTradedItem(client, Voucher(80, 10), true, store));

        Assert.Equal(100, habbo.Credits);
        Assert.DoesNotContain(sent, packet => packet.Header == ServerPacketHeader.CreditBalanceComposer);
    }

    [Fact]
    public void AConvertPaysWhatTheStoreRedeemedAndAFailedConvertChangesNothing()
    {
        var (habbo, client, sent) = Owner(Voucher(60, 10), Voucher(61, 5));
        var store = new ConvertStore { Redeem = items => [items.Single(item => item.ItemId == 60).ItemId] };

        new ConvertCreditsCommand(store).Execute(client, null!, []);

        Assert.Equal([new ExchangeRedemption(60, 10), new ExchangeRedemption(61, 5)], store.Requested.OrderBy(item => item.ItemId));
        Assert.Equal(110, habbo.Credits);
        Assert.Null(habbo.Inventory!.Furniture.GetItem(60));
        Assert.NotNull(habbo.Inventory.Furniture.GetItem(61));
        Assert.Contains(sent, packet => packet.Header == ServerPacketHeader.CreditBalanceComposer);

        sent.Clear();
        store.Redeem = _ => throw new InvalidOperationException("forced");
        new ConvertCreditsCommand(store).Execute(client, null!, []);

        Assert.Equal(110, habbo.Credits);
        Assert.NotNull(habbo.Inventory.Furniture.GetItem(61));
        Assert.DoesNotContain(sent, packet => packet.Header == ServerPacketHeader.CreditBalanceComposer);
    }

    [Theory]
    [InlineData(true, false, 100)]
    [InlineData(false, false, int.MaxValue)]
    [InlineData(false, true, 100)]
    public void ConvertKeepsCoinsWhenTheWalletOrItemIsUnavailable(bool closed, bool reserved, int credits)
    {
        var coin = Voucher(60, 10);
        var (habbo, client, sent) = Owner(coin);
        habbo.Credits = credits;
        habbo.CurrencyPersistenceUncertain = closed;
        if (reserved) {
            Assert.True(coin.TryReserve());
        }

        var store = new ConvertStore { Redeem = items => items.Select(item => item.ItemId).ToArray() };
        new ConvertCreditsCommand(store).Execute(client, null!, []);

        Assert.Empty(store.Requested);
        Assert.Same(coin, habbo.Inventory!.Furniture.GetItem(60));
        Assert.Equal(credits, habbo.Credits);
        Assert.DoesNotContain(sent, packet => packet.Header == ServerPacketHeader.CreditBalanceComposer);
    }

    [Fact]
    public void TradeHelperRejectsOverflowBeforeRedeeming()
    {
        var (habbo, client, _) = Owner();
        habbo.Credits = int.MaxValue;
        Assert.Throws<OverflowException>(() => Trade.ReceiveTradedItem(client, Voucher(80, 10), true, new FailingTradeStore()));
    }

    private static InventoryItem Voucher(uint id, int credits) => new()
    {
        Id = id,
        OwnerId = 1,
        Definition = new ItemDefinition { ItemName = "CF_" + credits + "_coin_gold", InteractionType = InteractionType.Exchange, BehaviourData = credits, Type = ItemType.Floor }
    };

    private static (Habbo Habbo, Plus.HabboHotel.GameClients.GameClient Client, List<(uint Header, byte[] Payload)> Sent) Owner(params InventoryItem[] items)
    {
        var habbo = new Habbo { Id = 1, Username = "owner", Credits = 100, Inventory = new InventoryComponent { Furniture = new(items, []) } };
        var (client, sent) = HabbiconTestSupport.Client(habbo);

        return (habbo, client, sent);
    }

    private sealed class FailingTradeStore : ITradeStore
    {
        public void DeleteItem(uint itemId) => throw new InvalidOperationException("forced");
        public void RedeemItem(uint itemId, int recipientId, int credits) => throw new InvalidOperationException("forced");
        public void TransferItem(uint itemId, int userId) => throw new InvalidOperationException("forced");
        public void Log(int firstUserId, int secondUserId, string firstItems, string secondItems) { }
        public bool Commit(IReadOnlyList<TradeTransfer> transfers, int firstUserId, int secondUserId, int firstCredits, int secondCredits,
            string firstItems, string secondItems, Func<bool> apply) => false;
    }

    private sealed class ConvertStore : IItemRedemptionStore
    {
        public List<ExchangeRedemption> Requested { get; } = [];
        public Func<IReadOnlyList<ExchangeRedemption>, IReadOnlyList<uint>> Redeem { get; set; } = _ => [];

        public IReadOnlyList<uint> RedeemInventoryExchanges(int ownerId, IReadOnlyList<ExchangeRedemption> items)
        {
            Requested.Clear();
            Requested.AddRange(items);

            return Redeem(items);
        }

        public void RedeemExchange(uint itemId, int ownerId, uint roomId, int credits) => throw new NotSupportedException();
        public IReadOnlyList<ClothingParts> ConsumeClothing(uint itemId, int ownerId, uint roomId, string name, IReadOnlyCollection<int> partIds) => throw new NotSupportedException();
        public AvatarEffect ConsumeEffectBox(uint itemId, int ownerId, uint roomId, int spriteId, DateTimeOffset now) => throw new NotSupportedException();
    }
}
