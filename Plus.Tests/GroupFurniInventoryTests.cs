using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Inventory.Furni;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class GroupFurniInventoryTests
{
    [Theory]
    [InlineData(InteractionType.GuildItem)]
    [InlineData(InteractionType.GuildGate)]
    [InlineData(InteractionType.GuildForum)]
    public async Task InventoryListsGroupFurniWithTheirGroupLook(InteractionType type)
    {
        var item = Item(type, groupId: 5);

        var entry = await ListedEntry(item, Groups(5));

        Assert.Equal(17, entry.Category);
        Assert.Equal(["0", "5", "b01010s21084", "84de00", "9844e7"], entry.Strings);
    }

    [Fact]
    public async Task GroupFurniOfAMissingGroupKeepItsStoredState()
    {
        var entry = await ListedEntry(Item(InteractionType.GuildItem, groupId: 9), Groups(5));

        Assert.Equal(17, entry.Category);
        Assert.Equal("0", entry.Legacy);
    }

    [Fact]
    public async Task OtherFurniKeepTheirCategoryAndData()
    {
        var entry = await ListedEntry(Item(InteractionType.None, groupId: 0), Groups(5));

        Assert.Equal(1, entry.Category);
        Assert.Equal("0", entry.Legacy);
    }

    private static InventoryItem Item(InteractionType type, int groupId) => new()
    {
        Id = 30,
        GroupId = groupId,
        ExtraData = new LegacyDataFormat { Data = "0" },
        Definition = new ItemDefinition
        {
            SpriteId = 4250,
            Type = ItemType.Floor,
            Category = FurniCategory.Default,
            InteractionType = type,
            AllowTrade = true,
            AllowInventoryStack = true
        }
    };

    internal static IGroupManager Groups(int groupId)
    {
        var group = new Group(groupId, "group", "", "b01010s21084", 1, 7, null, 0, 19, 37, 0, false, new GroupMembershipSnapshot([7], [], []));

        return new TestGroupManager(id => id == groupId ? group : null, (id, primary) => primary ? "84de00" : "9844e7");
    }

    private static async Task<Entry> ListedEntry(InventoryItem item, IGroupManager groups)
    {
        var habbo = new Habbo { Inventory = new() { Furniture = new([item], []) } };
        var (client, sent) = HabbiconTestSupport.Client(habbo);

        await new RequestFurniInventoryEvent(groups).Parse(client, HabbiconTestSupport.Incoming());

        var packet = Assert.Single(sent);
        Assert.Equal(ServerPacketHeader.FurniListComposer, packet.Header);
        var body = new FlashIncomingPacket { Buffer = packet.Payload };
        Assert.Equal(1, body.ReadInt());
        Assert.Equal(0, body.ReadInt());
        Assert.Equal(1, body.ReadInt());

        return Entry.Read(body);
    }

    internal sealed record Entry(int Category, string? Legacy, string[]? Strings)
    {
        public static Entry Read(FlashIncomingPacket body)
        {
            body.ReadUInt();
            body.ReadString();
            body.ReadUInt();
            body.ReadInt();
            var category = body.ReadInt();

            return body.ReadInt() switch
            {
                0 => new(category, body.ReadString(), null),
                2 => new(category, null, Enumerable.Range(0, body.ReadInt()).Select(_ => body.ReadString()).ToArray()),
                var other => throw new InvalidOperationException("data type " + other)
            };
        }
    }
}
