using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms.Games.Teams;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingTheFieldCancelsAnArmedSnowball(bool restart)
    {
        var (tile, _, _, _) = PlayedFreezeField();
        tile.InteractingUser = 7;
        tile.InteractionCountHelper = 1;
        tile.LegacyDataString = "1000";
        tile.RequestUpdate(4, true);
        var freeze = _room.GetFreeze();
        freeze.StopGame();

        if (restart) {
            freeze.StartGame();
        }

        _client.Packets.Clear();

        for (var tick = 0; tick < 4; tick++) {
            tile.ProcessUpdates();
        }

        Assert.DoesNotContain(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectUpdateComposer or ServerPacketHeader.ObjectsDataUpdateComposer);
    }

    [Fact]
    public void EndingAFreezeGameKeepsTheTilesAndClosesTheBrokenBlocks()
    {
        var (tile, broken, whole, exit) = PlayedFreezeField();

        _room.GetFreeze().StopGame();

        Assert.Equal(("11100", "11000", "", "0"), (tile.LegacyDataString, broken.LegacyDataString, whole.LegacyDataString, exit.LegacyDataString));
    }

    [Fact]
    public void EndingAFreezeGameSendsItsFieldInOneUpdate()
    {
        PlayedFreezeField();
        _client.Packets.Clear();

        _room.GetFreeze().StopGame();

        var update = Assert.Single(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectsDataUpdateComposer
            or ServerPacketHeader.ObjectDataUpdateComposer or ServerPacketHeader.ObjectUpdateComposer);
        Assert.Equal((ServerPacketHeader.ObjectsDataUpdateComposer, 2), (update.Header, BinaryPrimitives.ReadInt32BigEndian(update.Body)));
    }

    private (Item Tile, Item Broken, Item Whole, Item Exit) PlayedFreezeField()
    {
        var tile = FieldItem(FreezeTile(10, 1, 1));
        var brokenTile = FieldItem(FreezeTile(11, 2, 1));
        var wholeTile = FieldItem(FreezeTile(12, 1, 2));
        var broken = FieldItem(Add(20, 2, 1, type: InteractionType.FreezeTileBlock));
        var whole = FieldItem(Add(21, 1, 2, type: InteractionType.FreezeTileBlock));
        var exit = FieldItem(Add(30, 3, 3, type: InteractionType.Freezeexit));
        var freeze = _room.GetFreeze();
        freeze.AddFreezeBlock(broken);
        freeze.AddFreezeBlock(whole);
        FreezePlayer(Viewer(0, 3), Team.Red);
        FreezePlayer(SecondFreezeUser(3, 2), Team.Blue);
        freeze.StartGame();
        tile.LegacyDataString = "11100";
        brokenTile.LegacyDataString = "11200";
        broken.LegacyDataString = "1200";
        _ = wholeTile;

        return (tile, broken, whole, exit);
    }

    private static Item FieldItem(Item item)
    {
        item.ExtraData = new LegacyDataFormat();

        return item;
    }
}
