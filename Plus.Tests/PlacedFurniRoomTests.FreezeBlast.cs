using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Rooms.Games.Teams;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void AFreezeBlastReachesEachTileAfterAHundredMillisecondsPerStep()
    {
        var centre = DataTile(10, 1, 1);
        var east1 = DataTile(11, 2, 1);
        var east2 = DataTile(12, 3, 1);
        var west1 = DataTile(13, 0, 1);
        var south1 = DataTile(14, 1, 2);
        var south2 = DataTile(15, 1, 3);
        var aside = DataTile(16, 2, 2);
        FreezePlayer(Viewer(3, 3), Team.Red);
        FreezePlayer(SecondFreezeUser(0, 3), Team.Blue);
        var freeze = _room.GetFreeze();
        freeze.StartGame();

        freeze.OnFreezeTiles(centre, FreezePowerUp.None);

        Assert.Equal(["11100", "11100", "11200", "11100", "11100", "11200", ""],
            new[] { centre, east1, east2, west1, south1, south2, aside }.Select(tile => tile.LegacyDataString));
    }

    [Fact]
    public void AnIceBlockInAFreezeBlastBreaksWithTheDelayOfItsTile()
    {
        var centre = DataTile(10, 1, 1);
        DataTile(11, 2, 1);
        DataTile(12, 3, 1);
        var block = WithData(Add(20, 3, 1, type: InteractionType.FreezeTileBlock));
        _room.GetFreeze().AddFreezeBlock(block);
        FreezePlayer(Viewer(3, 3), Team.Red);
        FreezePlayer(SecondFreezeUser(0, 3), Team.Blue);
        var freeze = _room.GetFreeze();
        freeze.StartGame();

        freeze.OnFreezeTiles(centre, FreezePowerUp.None);

        var data = int.Parse(block.LegacyDataString);
        Assert.Equal(200, data % 1000);
        Assert.InRange(data / 1000, 1, 7);
    }

    [Fact]
    public void APlayerAFreezeBlastHitsFreezesWithTheDelayOfTheTile()
    {
        var centre = DataTile(10, 1, 1);
        DataTile(11, 2, 1);
        DataTile(12, 3, 1);
        var red = FreezePlayer(Viewer(3, 1), Team.Red);
        _client.GetHabbo().Effects.Init(_client.GetHabbo());
        FreezePlayer(SecondFreezeUser(0, 3), Team.Blue);
        var freeze = _room.GetFreeze();
        freeze.StartGame();
        _client.Packets.Clear();

        freeze.OnFreezeTiles(centre, FreezePowerUp.None);

        var effect = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.AvatarEffectComposer);
        Assert.Equal((red.VirtualId, 12, 200), (BinaryPrimitives.ReadInt32BigEndian(effect.Body), BinaryPrimitives.ReadInt32BigEndian(effect.Body.AsSpan(4)),
            BinaryPrimitives.ReadInt32BigEndian(effect.Body.AsSpan(8))));
    }

    [Fact]
    public void AFreezeBlastSendsItsTilesAndBlocksInOneUpdate()
    {
        var centre = DataTile(10, 1, 1);
        DataTile(11, 2, 1);
        DataTile(12, 3, 1);
        var block = WithData(Add(20, 3, 1, type: InteractionType.FreezeTileBlock));
        _room.GetFreeze().AddFreezeBlock(block);
        FreezePlayer(Viewer(3, 3), Team.Red);
        FreezePlayer(SecondFreezeUser(0, 3), Team.Blue);
        var freeze = _room.GetFreeze();
        freeze.StartGame();
        _client.Packets.Clear();

        freeze.OnFreezeTiles(centre, FreezePowerUp.None);

        var update = Assert.Single(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectsDataUpdateComposer
            or ServerPacketHeader.ObjectDataUpdateComposer or ServerPacketHeader.ObjectUpdateComposer);
        Assert.Equal(ServerPacketHeader.ObjectsDataUpdateComposer, update.Header);
        Assert.Equal(4, BinaryPrimitives.ReadInt32BigEndian(update.Body));
    }

    private Item DataTile(uint id, int x, int y) => WithData(FreezeTile(id, x, y));

    private static Item WithData(Item item)
    {
        item.ExtraData = new LegacyDataFormat();

        return item;
    }
}
