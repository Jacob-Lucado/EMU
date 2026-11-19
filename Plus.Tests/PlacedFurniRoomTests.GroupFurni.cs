using Plus.Communication.Flash;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(InteractionType.GuildItem)]
    [InlineData(InteractionType.GuildGate)]
    [InlineData(InteractionType.GuildForum)]
    public async Task GroupFurniPlacedFromTheInventoryShowsItsGroup(InteractionType type)
    {
        GuildLook(5);
        Viewer();
        Inventory(new InventoryItem { Id = 30, Definition = Furni(30, type, WiredBoxType.None).Definition, GroupId = 5 });

        await PlaceObject().Parse(_room, _client, ClientPacket("30 1 1 0"));

        var packet = Assert.Single(_client.Packets, sent => sent.Header == ServerPacketHeader.ObjectAddComposer);
        var body = new FlashIncomingPacket { Buffer = packet.Body };
        Assert.Equal(30u, body.ReadUInt());
        body.ReadInt();
        body.ReadInt();
        body.ReadInt();
        body.ReadInt();
        body.ReadString();
        body.ReadString();
        body.ReadInt();
        Assert.Equal(2, body.ReadInt());
        Assert.Equal(5, body.ReadInt());
        Assert.Equal(["0", "5", "b01010s21084", "84de00", "9844e7"], Enumerable.Range(0, 5).Select(_ => body.ReadString()));
    }

    [Fact]
    public async Task PickedUpGroupFurniKeepsItsGroupInTheInventory()
    {
        GuildLook(5);
        Viewer();
        var item = Add(30, 1, 1, type: InteractionType.GuildItem);
        item.GroupId = 5;

        await PickupObject().Parse(_client, ClientPacket(2, 30));

        var picked = Assert.IsType<InventoryComponent>(_client.GetHabbo().Inventory).Furniture.GetItem(30);
        Assert.Equal(5, picked.GroupId);

        await PlaceObject().Parse(_room, _client, ClientPacket("30 2 2 0"));

        Assert.Equal(5, _room.GetRoomItemHandler().GetItem(30).GroupId);
    }

    [Fact]
    public async Task PickedUpGroupFurniReachesTheInventoryWithItsGroupLook()
    {
        GuildLook(5);
        Viewer();
        var item = Add(30, 1, 1, type: InteractionType.GuildItem);
        item.GroupId = 5;
        _client.Packets.Clear();

        await PickupObject().Parse(_client, ClientPacket(2, 30));

        var added = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.FurniListAddComposer);
        var body = new FlashIncomingPacket { Buffer = added.Body };
        Assert.Equal(1, body.ReadInt());
        var entry = GroupFurniInventoryTests.Entry.Read(body);
        Assert.Equal(17, entry.Category);
        Assert.Equal(["5", "b01010s21084", "84de00", "9844e7"], entry.Strings!.Skip(1));
    }

    private void GuildLook(int groupId)
    {
        var group = new Group(groupId, "group", "", "b01010s21084", RoomId, 7, null, 0, 19, 37, 0, false, new GroupMembershipSnapshot([7], [], []));
        Set("_groups", Proxy<IGroupManager>((method, args) =>
        {
            switch (method) {
                case "TryGetGroup":
                    if ((int)args[0]! != groupId) {
                        return false;
                    }

                    args[1] = group;

                    return true;
                case "GetColourCode":
                    return (bool)args[1]! ? "84de00" : "9844e7";
                default:
                    throw new InvalidOperationException(method);
            }
        }));
    }
}
