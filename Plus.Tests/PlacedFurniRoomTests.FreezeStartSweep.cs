using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void AFreezeStartSendsAPlayerWithoutATeamOffTheField()
    {
        FreezeTile(10, 1, 1);
        FreezeTile(11, 2, 1);
        var exit = Add(30, 0, 3, type: InteractionType.Freezeexit);
        var freeze = _room.GetFreeze();
        freeze.AddExitTile(exit);
        var spectator = Viewer(2, 1);
        _room.GetGameMap().AddUserToMap(spectator, spectator.Coordinate);
        _client.GetHabbo().Effects.Init(_client.GetHabbo());
        FreezePlayer(SecondFreezeUser(1, 1), Team.Red);
        FreezePlayer(ThirdFreezeUser(3, 3), Team.Blue);
        _client.Packets.Clear();

        freeze.StartGame();
        var effects = new List<int>(SweepEffects());
        var positions = new List<(int, int)>();

        for (var tick = 0; tick < 7; tick++) {
            freeze.CycleUser(spectator);

            using (RoomOwnerScope.Enter(_room)) {
                _room.GetGameMap().Navigation!.DrainCommands();
            }

            positions.Add((spectator.X, spectator.Y));
        }

        Assert.Equal([4], effects);
        Assert.Equal([(2, 1), (2, 1), (2, 1), (0, 3), (0, 3), (0, 3), (0, 3)], positions);
        Assert.Equal([4, 0], SweepEffects());
    }

    [Fact]
    public void AFreezeStartLeavesAPlayerWithoutATeamOffTheFieldAlone()
    {
        FreezeTile(10, 1, 1);
        var spectator = Viewer(3, 3);
        _client.GetHabbo().Effects.Init(_client.GetHabbo());
        FreezePlayer(SecondFreezeUser(1, 1), Team.Red);
        FreezePlayer(ThirdFreezeUser(3, 2), Team.Blue);
        _client.Packets.Clear();

        _room.GetFreeze().StartGame();

        Assert.Empty(SweepEffects());
    }

    private List<int> SweepEffects() => [.. _client.Packets.Where(packet => packet.Header == ServerPacketHeader.AvatarEffectComposer
        && BinaryPrimitives.ReadInt32BigEndian(packet.Body) == 1).Select(packet => BinaryPrimitives.ReadInt32BigEndian(packet.Body.AsSpan(4)))];

    private RoomUser ThirdFreezeUser(int x, int y)
    {
        var client = new TestClient();
        client.SetHabbo(new Habbo { Id = 10, Username = "third", CurrentRoom = _room, Access = UserAccess.Empty });
        var user = new RoomUser(10, RoomId, 3, _room, client, TestChatEmotions.Unused, new TestRewardProgress()) { X = x, Y = y };
        var users = (ConcurrentDictionary<int, RoomUser>)typeof(RoomUserManager).GetField("_users", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_room.GetRoomUserManager())!;
        users.TryAdd(3, user);

        return user;
    }
}
