using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Data.Moodlight;
using Plus.HabboHotel.Items.Data.Toner;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

// A background toner works as soon as it is placed: the room sends its stored [state, hue, saturation, lightness],
// and the owner can switch it and set its color without a room reload. Picking it up frees the room for the next toner.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task APlacedTonerSendsItsStoredColorAndCanBeSwitchedAndColored()
    {
        var store = UseTonerStore();
        store.Rows[80] = new() { Enabled = true, Hue = 10, Saturation = 20, Lightness = 30 };
        Viewer(3, 3);

        var toner = await PlaceToner(80);

        Assert.Equal([1, 10, 20, 30], TonerData(Assert.Single(Sent(ServerPacketHeader.ObjectAddComposer))));

        await UseToner(store, toner);
        Assert.Equal([0, 10, 20, 30], TonerData(Sent(ServerPacketHeader.ObjectUpdateComposer).Last()));

        new RoomItemMetadataService(store, null!).SetToner(_room, _client, new(toner.Id, 40, 50, 60));
        Assert.Equal([1, 40, 50, 60], TonerData(Sent(ServerPacketHeader.ObjectUpdateComposer).Last()));
        Assert.Equal((true, 40, 50, 60), (store.Rows[80].Enabled, store.Rows[80].Hue, store.Rows[80].Saturation, store.Rows[80].Lightness));
    }

    [Fact]
    public async Task AfterAPickupTheNextPlacedTonerCanBeSwitched()
    {
        var store = UseTonerStore();
        Viewer(3, 3);
        await PlaceToner(81);
        await PickUpToner(store, 81);

        var next = await PlaceToner(82);
        await UseToner(store, next);

        Assert.Equal([1, 0, 0, 0], TonerData(Sent(ServerPacketHeader.ObjectUpdateComposer).Last()));
        Assert.True(store.Rows[82].Enabled);
    }

    [Fact]
    public async Task ATonerPlacedAgainAfterAPickupSendsItsStoredDataAndCanBeSwitched()
    {
        var store = UseTonerStore();
        store.Rows[83] = new() { Enabled = true, Hue = 10, Saturation = 20, Lightness = 30 };
        Viewer(3, 3);
        await PlaceToner(83);
        await PickUpToner(store, 83);

        var again = await PlaceToner(83);

        Assert.Equal([0, 0, 0, 0], TonerData(Sent(ServerPacketHeader.ObjectAddComposer).Last()));
        await UseToner(store, again);
        Assert.Equal([1, 0, 0, 0], TonerData(Sent(ServerPacketHeader.ObjectUpdateComposer).Last()));
    }

    [Fact]
    public async Task SwitchingOrColoringATonerSendsOneObjectUpdate()
    {
        var store = UseTonerStore();
        Viewer(3, 3);
        var toner = await PlaceToner(84);
        _client.Packets.Clear();

        await UseToner(store, toner);
        Assert.Single(Sent(ServerPacketHeader.ObjectUpdateComposer));

        _client.Packets.Clear();
        new RoomItemMetadataService(store, null!).SetToner(_room, _client, new(toner.Id, 1, 2, 3));
        Assert.Single(Sent(ServerPacketHeader.ObjectUpdateComposer));
    }

    private TonerStore UseTonerStore()
    {
        var store = new TonerStore();
        Set("_roomItemHandling", new RoomItemHandling(_room, TestRoomItemStore.Instance, store, TestGameClientManager.Empty, TestLanguageManager.RoomItems,
            TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));

        return store;
    }

    private async Task<Item> PlaceToner(uint id)
    {
        var definition = Furni(id, InteractionType.Toner, WiredBoxType.None).Definition;
        _client.GetHabbo().Inventory = new InventoryComponent { Furniture = new FurnitureInventoryComponent([new InventoryItem { Id = id, Definition = definition }], []) };

        await PlaceObject().Parse(_room, _client, ClientPacket($"{id} 1 1 0"));

        return Assert.IsType<Item>(_room.GetRoomItemHandler().GetItem(id));
    }

    // Picking a toner up deletes its stored row, as RoomItemPickupStore does.
    private async Task PickUpToner(TonerStore store, uint id)
    {
        await PickupObject().Parse(_client, ClientPacket(0, (int)id));
        Assert.Null(_room.GetRoomItemHandler().GetItem(id));
        store.Rows.Remove(id);
    }

    private async Task UseToner(TonerStore store, Item toner) =>
        await new UseFurnitureEvent(new FurnitureUseService(store, TestItemRuntime.Quests)).Parse(_room, _client, ClientPacket((int)toner.Id, 0));

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
        Assert.Equal(5, packet.ReadInt());

        return Enumerable.Range(0, packet.ReadInt()).Select(_ => packet.ReadInt()).ToArray();
    }

    private sealed class TonerStore : IRoomItemMetadataStore, IFurnitureUseStore
    {
        public Dictionary<uint, TonerRecord> Rows { get; } = [];

        public TonerRecord? LoadToner(uint itemId)
        {
            if (!Rows.TryGetValue(itemId, out var row)) {
                Rows[itemId] = row = new() { Enabled = false, Hue = 0, Saturation = 0, Lightness = 0 };
            }

            return row;
        }

        public void SetToner(uint itemId, uint roomId, int hue, int saturation, int lightness) =>
            Rows[itemId] = Rows.ContainsKey(itemId)
                ? new() { Enabled = true, Hue = hue, Saturation = saturation, Lightness = lightness }
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
