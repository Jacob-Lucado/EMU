using System.Text;
using Plus.Communication.Packets.Incoming.Rooms.Furni;
using Plus.Communication.Packets.Outgoing;
using Plus.Core.Settings;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Users.Clothing.Parts;
using Plus.HabboHotel.Users.Effects;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    private const uint CoinId = 60;

    // Habbo answers a redeem with only the room removal (and its height map update) and the new balance. The removal names no picker,
    // so the coin vanishes instead of flying to the inventory.
    [Fact]
    public async Task RedeemingACoinRemovesItWithoutAPickerAndSendsOnlyTheNewBalance()
    {
        var store = PrepareCoin(credits: 1);
        _client.GetHabbo().Credits = 196;

        await new CreditFurniRedeemEvent(new ItemRedemptionService(store, Proxy<ISettingsManager>((_, _) => "1"))).Parse(_room, _client, ClientPacket((int)CoinId));

        Assert.Equal([CoinId], store.Deleted);
        Assert.Null(_room.GetRoomItemHandler().GetItem(CoinId));
        Assert.Equal(197, _client.GetHabbo().Credits);
        Assert.Equal(
            [ServerPacketHeader.CreditBalanceComposer, ServerPacketHeader.HeightMapUpdateComposer, ServerPacketHeader.ObjectRemoveComposer],
            _client.Sent.Order());
        Assert.Equal(Text("60").Append((byte)0).Concat(Ints(-1, 0)).Append((byte)0), Body(ServerPacketHeader.ObjectRemoveComposer));
        Assert.Equal(Text("197.0"), Body(ServerPacketHeader.CreditBalanceComposer));
    }

    private CoinStore PrepareCoin(int credits)
    {
        AdmitOwner();
        var coin = Furni(CoinId, InteractionType.Exchange, WiredBoxType.None);
        coin.Definition.ItemName = "CF_1_coin_bronze";
        coin.Definition.BehaviourData = credits;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null, coin, 1, 1, 0, true, false, false));
        _client.Sent.Clear();
        _client.Packets.Clear();

        return new CoinStore();
    }

    private static byte[] Text(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);

        return [(byte)(bytes.Length >> 8), (byte)bytes.Length, .. bytes];
    }

    private sealed class CoinStore : IItemRedemptionStore
    {
        public List<uint> Deleted { get; } = [];

        public void RedeemExchange(uint itemId, int ownerId, uint roomId, int balance) => Deleted.Add(itemId);

        public IReadOnlyList<uint> RedeemInventoryExchanges(int ownerId, int startingBalance, IReadOnlyList<ExchangeRedemption> items) => throw new NotSupportedException();

        public IReadOnlyList<ClothingParts> ConsumeClothing(uint itemId, int ownerId, uint roomId, string name, IReadOnlyCollection<int> partIds) =>
            throw new NotSupportedException();

        public AvatarEffect ConsumeEffectBox(uint itemId, int ownerId, uint roomId, int spriteId, DateTimeOffset now) => throw new NotSupportedException();
    }
}
