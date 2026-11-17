using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.PathFinding;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void TheHitThatTakesTheLastFreezeLifeOnlyFreezesThePlayer()
    {
        var (red, freeze) = LastLifeHit();

        Assert.True(red.Freezed);
        Assert.Equal(Team.Red, red.Team);
        Assert.True(freeze.GameIsStarted);
        Assert.Equal(20, _room.GetGameManager().Points[(int)Team.Red]);
    }

    [Fact]
    public void APlayerWithoutLivesLeavesTheGameWhenTheFreezeEnds()
    {
        var (red, freeze) = LastLifeHit();
        _client.Packets.Clear();

        for (var tick = 0; tick < 10; tick++) {
            freeze.CycleUser(red);
        }

        Assert.Equal(Team.Red, red.Team);
        freeze.CycleUser(red);

        Assert.Equal(Team.None, red.Team);
        Assert.DoesNotContain(red, _room.GetTeamManagerForFreeze().RedTeam);
        Assert.Equal(10, _room.GetGameManager().Points[(int)Team.Red]);
        Assert.False(freeze.GameIsStarted);
        Assert.Contains(_client.Packets, packet => packet.Header == ServerPacketHeader.YouArePlayingGameComposer);
        Assert.Equal([4], Effects());
    }

    [Fact]
    public void AnEliminatedFreezePlayerMovesToTheExitAfterFourTicksAndLosesTheEffectAfterSeven()
    {
        var (red, freeze) = LastLifeHit();
        ElimExit(31, 0, 3);

        for (var tick = 0; tick < 11; tick++) {
            freeze.CycleUser(red);
        }

        _client.Packets.Clear();
        var positions = new List<(int, int)>();

        for (var tick = 0; tick < 7; tick++) {
            freeze.CycleUser(red);

            using (RoomOwnerScope.Enter(_room)) {
                _room.GetGameMap().Navigation!.DrainCommands();
            }

            positions.Add((red.X, red.Y));
        }

        Assert.Equal([(2, 1), (2, 1), (2, 1), (0, 3), (0, 3), (0, 3), (0, 3)], positions);
        Assert.Equal([0], Effects());
    }

    private (RoomUser Red, Freeze Freeze) LastLifeHit()
    {
        var tile = FreezeTile(10, 1, 1);
        FreezeTile(11, 2, 1);
        var red = FreezePlayer(Viewer(2, 1), Team.Red);
        _client.GetHabbo().Effects.Init(_client.GetHabbo());
        FreezePlayer(SecondFreezeUser(3, 3), Team.Blue);
        var freeze = _room.GetFreeze();
        freeze.StartGame();
        red.FreezeLives = 1;
        _room.GetGameManager().Points[(int)Team.Red] = 30;
        freeze.OnFreezeTiles(tile, FreezePowerUp.None);

        return (red, freeze);
    }

    private Item ElimExit(uint id, int x, int y)
    {
        var exit = Add(id, x, y, type: InteractionType.Freezeexit);
        _room.GetFreeze().AddExitTile(exit);

        return exit;
    }

    private List<int> Effects() => [.. _client.Packets.Where(packet => packet.Header == ServerPacketHeader.AvatarEffectComposer)
        .Select(packet => BinaryPrimitives.ReadInt32BigEndian(packet.Body.AsSpan(4)))];
}
