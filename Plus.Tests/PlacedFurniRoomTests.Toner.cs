using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Data.Moodlight;
using Plus.HabboHotel.Items.Data.Toner;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

// A background toner carries [state, hue, saturation, lightness]. The owner sets its colour, anyone with rights switches it,
// and the data stays with the furni through a pickup.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task ANewTonerIsPlacedOffWithEveryChannelAt127()
    {
        UseTonerStore();
        Viewer(3, 3);

        await PlaceToner(80);

        Assert.Equal([0, 127, 127, 127], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectAddComposer))));
    }

    [Fact]
    public async Task TheOwnerSetsTheColourWithAFullObjectUpdateAndTheOnOffStateStays()
    {
        var store = UseTonerStore();
        store.Rows[81] = new() { Enabled = false, Hue = 1, Saturation = 2, Lightness = 3 };
        Viewer(3, 3);
        var toner = await PlaceToner(81);
        _client.Packets.Clear();

        SetTonerColour(store, toner, 100, 200, 120);

        Assert.Equal([0, 100, 200, 120], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectUpdateComposer))));
        Assert.Empty(Sent(ServerPacketHeader.ObjectDataUpdateComposer));
        Assert.Equal((false, 100, 200, 120), Row(store, 81));

        await UseToner(store, toner);
        _client.Packets.Clear();
        SetTonerColour(store, toner, 30, 40, 50);

        Assert.Equal([1, 30, 40, 50], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectUpdateComposer))));
        Assert.Equal((true, 30, 40, 50), Row(store, 81));
    }

    [Fact]
    public async Task ColourValuesOutsideZeroTo255AreClamped()
    {
        var store = UseTonerStore();
        Viewer(3, 3);
        var toner = await PlaceToner(82);
        _client.Packets.Clear();

        SetTonerColour(store, toner, 300, -1, 256);

        Assert.Equal([0, 255, 0, 255], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectUpdateComposer))));
        Assert.Equal((false, 255, 0, 255), Row(store, 82));
    }

    [Fact]
    public async Task UsingATonerTogglesItWithOneObjectDataUpdateCarryingTheFullData()
    {
        var store = UseTonerStore();
        store.Rows[83] = new() { Enabled = false, Hue = 30, Saturation = 40, Lightness = 50 };
        Viewer(3, 3);
        var toner = await PlaceToner(83);
        _client.Packets.Clear();

        await UseToner(store, toner);

        Assert.Equal([1, 30, 40, 50], ObjectData(Assert.Single(Sent(ServerPacketHeader.ObjectDataUpdateComposer)), 83));
        Assert.Empty(Sent(ServerPacketHeader.ObjectUpdateComposer));
        Assert.Equal((true, 30, 40, 50), Row(store, 83));

        _client.Packets.Clear();
        await UseToner(store, toner);

        Assert.Equal([0, 30, 40, 50], ObjectData(Assert.Single(Sent(ServerPacketHeader.ObjectDataUpdateComposer)), 83));
        Assert.Equal((false, 30, 40, 50), Row(store, 83));
    }

    [Fact]
    public async Task AUserWithRightsMaySwitchATonerButNotColourIt()
    {
        var store = UseTonerStore();
        store.Rows[84] = new() { Enabled = false, Hue = 30, Saturation = 40, Lightness = 50 };
        Viewer(3, 3);
        var toner = await PlaceToner(84);
        _room.OwnerId = 99;
        _room.UsersWithRights = [7];
        _client.Packets.Clear();

        SetTonerColour(store, toner, 10, 20, 30);

        Assert.Empty(_client.Packets);
        Assert.Equal((false, 30, 40, 50), Row(store, 84));

        await UseToner(store, toner);

        Assert.Equal([1, 30, 40, 50], ObjectData(Assert.Single(Sent(ServerPacketHeader.ObjectDataUpdateComposer)), 84));
        Assert.Equal((true, 30, 40, 50), Row(store, 84));
    }

    [Fact]
    public async Task AUserWithoutRightsCanNeitherSwitchNorColourAToner()
    {
        var store = UseTonerStore();
        store.Rows[85] = new() { Enabled = true, Hue = 30, Saturation = 40, Lightness = 50 };
        Viewer(3, 3);
        var toner = await PlaceToner(85);
        _room.OwnerId = 99;
        _room.UsersWithRights = [];
        _client.Packets.Clear();

        await UseToner(store, toner);
        SetTonerColour(store, toner, 10, 20, 30);

        Assert.Empty(_client.Packets);
        Assert.Equal((true, 30, 40, 50), Row(store, 85));
    }

    [Fact]
    public async Task APickedUpTonerKeepsItsDataInTheInventoryAndWhenPlacedAgain()
    {
        var store = UseTonerStore();
        store.Rows[86] = new() { Enabled = true, Hue = 255, Saturation = 0, Lightness = 255 };
        Viewer(3, 3);
        await PlaceToner(86);

        await PickUpToner(86);

        var inventoryItem = Assert.IsType<InventoryItem>(_client.GetHabbo().Inventory.Furniture.GetItem(86));
        Assert.Equal([1, 255, 0, 255], Assert.IsType<IntArrayDataFormat>(inventoryItem.ExtraData).Data);

        _client.Packets.Clear();
        var again = await PlaceToner(86, inventoryItem);

        Assert.Equal([1, 255, 0, 255], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectAddComposer))));
        await UseToner(store, again);
        Assert.Equal([0, 255, 0, 255], ObjectData(Sent(ServerPacketHeader.ObjectDataUpdateComposer).Last(), 86));
    }

    [Fact]
    public async Task AfterAPickupTheNextPlacedTonerCanBeSwitched()
    {
        var store = UseTonerStore();
        Viewer(3, 3);
        await PlaceToner(87);
        await PickUpToner(87);

        var next = await PlaceToner(88);
        _client.Packets.Clear();
        await UseToner(store, next);

        Assert.Equal([1, 127, 127, 127], ObjectData(Assert.Single(Sent(ServerPacketHeader.ObjectDataUpdateComposer)), 88));
        Assert.True(store.Rows[88].Enabled);
    }

    [Fact]
    public void AnInventoryTonerLoadsItsStoredData()
    {
        var definition = Furni(89, InteractionType.Toner, WiredBoxType.None).Definition;

        var data = FurniExtraData.Load(definition, "1\n255\n0\n255", keepLegacy: false);

        Assert.Equal([1, 255, 0, 255], Assert.IsType<IntArrayDataFormat>(data).Data);
    }

    private TonerStore UseTonerStore()
    {
        var store = new TonerStore();
        Set("_roomItemHandling", new RoomItemHandling(_room, TestRoomItemStore.Instance, store, TestGameClientManager.Empty, TestLanguageManager.RoomItems,
            TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));

        return store;
    }

    private async Task<Item> PlaceToner(uint id, InventoryItem? item = null)
    {
        item ??= new InventoryItem { Id = id, Definition = Furni(id, InteractionType.Toner, WiredBoxType.None).Definition };
        _client.GetHabbo().Inventory = new InventoryComponent { Furniture = new FurnitureInventoryComponent([item], []) };

        await PlaceObject().Parse(_room, _client, ClientPacket($"{id} 1 1 0"));

        return Assert.IsType<Item>(_room.GetRoomItemHandler().GetItem(id));
    }

    private async Task PickUpToner(uint id)
    {
        await PickupObject().Parse(_client, ClientPacket(0, (int)id));
        Assert.Null(_room.GetRoomItemHandler().GetItem(id));
    }

    private async Task UseToner(TonerStore store, Item toner) =>
        await new UseFurnitureEvent(new FurnitureUseService(store, TestItemRuntime.Quests)).Parse(_room, _client, ClientPacket((int)toner.Id, 0));

    private void SetTonerColour(TonerStore store, Item toner, int hue, int saturation, int lightness) =>
        new RoomItemMetadataService(store, null!).SetToner(_room, _client, new(toner.Id, hue, saturation, lightness));

    private static (bool, int, int, int) Row(TonerStore store, uint id) =>
        (store.Rows[id].Enabled, store.Rows[id].Hue, store.Rows[id].Saturation, store.Rows[id].Lightness);

    private List<byte[]> Sent(uint header) => _client.Packets.Where(packet => packet.Header == header).Select(packet => packet.Body).ToList();

    // Floor object layout: id, sprite, x, y, rotation, z, stack height, extra, then the stuff data.
    private static int[] TonerData(byte[] body)
    {
        var packet = new FlashIncomingPacket { Buffer = body };
        packet.ReadInt();
        packet.ReadInt();
        packet.ReadInt();
        packet.ReadInt();
        packet.ReadInt();
        packet.ReadString();
        packet.ReadString();
        packet.ReadInt();

        return IntArray(packet);
    }

    // Object data update layout: the id as a string, then the stuff data.
    private static int[] ObjectData(byte[] body, uint id)
    {
        var packet = new FlashIncomingPacket { Buffer = body };
        Assert.Equal(id.ToString(), packet.ReadString());

        return IntArray(packet);
    }

    private static int[] IntArray(FlashIncomingPacket packet)
    {
        Assert.Equal(5, packet.ReadInt());

        return Enumerable.Range(0, packet.ReadInt()).Select(_ => packet.ReadInt()).ToArray();
    }

    private sealed class TonerStore : IRoomItemMetadataStore, IFurnitureUseStore
    {
        public Dictionary<uint, TonerRecord> Rows { get; } = [];

        public TonerRecord? LoadToner(uint itemId)
        {
            if (!Rows.TryGetValue(itemId, out var row)) {
                Rows[itemId] = row = TonerRecord.New;
            }

            return row;
        }

        public void SetToner(uint itemId, uint roomId, int hue, int saturation, int lightness) =>
            Rows[itemId] = Rows.TryGetValue(itemId, out var row)
                ? new() { Enabled = row.Enabled, Hue = hue, Saturation = saturation, Lightness = lightness }
                : throw new InvalidOperationException("Toner data was not persisted.");

        public void SetTonerEnabled(uint itemId, uint roomId, bool enabled) =>
            Rows[itemId] = Rows.TryGetValue(itemId, out var row)
                ? new() { Enabled = enabled, Hue = row.Hue, Saturation = row.Saturation, Lightness = row.Lightness }
                : throw new InvalidOperationException("Toner state was not persisted.");

        public void SetMannequinData(uint itemId, uint roomId, string data) => throw new NotSupportedException();
        public void SetBrandingData(uint itemId, uint roomId, string data) => throw new NotSupportedException();
        public MoodlightRecord? LoadMoodlight(uint itemId) => throw new NotSupportedException();
        public void SetMoodlightEnabled(uint itemId, uint roomId, bool enabled) => throw new NotSupportedException();
        public void UpdateMoodlightPreset(uint itemId, uint roomId, int preset, string value) => throw new NotSupportedException();
    }
}
