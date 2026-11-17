using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WalkingOntoTheDoorTileLeavesTheRoomOnlyWhenTheRoomSaysSo(bool leaveOnDoorTile, bool stays)
    {
        Set("_gamemap", new Gamemap(_room, new RoomModel("door_east", 4, 1, 0, 6, DoorEastRoom, 0, 0, false), TestLogging.Navigation,
            TestRoomSettings.Empty, new TestGroupManager(id => _groupLookup(id)), _database, TestNavigationRewards.Instance));
        _room.GetGameMap().GenerateMaps();
        _room.Behavior = RoomBehaviorSettings.Default with { LeaveOnDoorTile = leaveOnDoorTile };
        var actor = FallbackActor(2, 1);
        await EdgeMove().Parse(_client, ClientPacket(4, 1));

        for (var tick = 0; tick < 8; tick++) {
            ExecutorTick();
        }

        Assert.Equal(stays, _room.GetRoomUserManager().GetRoomUserByVirtualId(actor.VirtualId) == actor);
        Assert.Equal(stays, _client.GetHabbo().CurrentRoom == _room);

        if (stays) {
            Assert.Equal((4, 1), (actor.X, actor.Y));
            Assert.False(actor.HasStatus("mv"));
        }
    }

    [Fact]
    public void AnAvatarFallsAsleepAtTheRoomIdleSleepTimeout()
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleSleepTimeoutSeconds = 30 };
        var actor = ExecutorActor(0, 1);
        actor.IdleTime = 58;

        ExecutorTick();
        Assert.False(actor.IsAsleep);
        Assert.DoesNotContain(ServerPacketHeader.SleepComposer, _client.Sent);

        ExecutorTick();
        Assert.True(actor.IsAsleep);
        Assert.Contains(ServerPacketHeader.SleepComposer, _client.Sent);
    }

    [Fact]
    public void TheDefaultIdleSleepTimeoutIsFiveMinutes()
    {
        var actor = ExecutorActor(0, 1);
        actor.IdleTime = 598;

        ExecutorTick();
        Assert.False(actor.IsAsleep);

        ExecutorTick();
        Assert.True(actor.IsAsleep);
    }

    [Fact]
    public void WithIdleSleepOffAnIdleAvatarStaysAwake()
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleSleepEnabled = false, IdleAutokickEnabled = false };
        var actor = ExecutorActor(0, 1);
        actor.IdleTime = 100_000;

        ExecutorTick();

        Assert.False(actor.IsAsleep);
        Assert.DoesNotContain(ServerPacketHeader.SleepComposer, _client.Sent);
    }

    [Theory]
    [InlineData(60, 120)]
    [InlineData(900, 1800)]
    public void AVisitorIsKickedOnceIdleForTheRoomAutokickTimeout(int seconds, int ticks)
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleAutokickTimeoutSeconds = seconds };
        var actor = ExecutorActor(0, 1);
        _room.OwnerId = 99;
        actor.IdleTime = ticks - 1;

        ExecutorTick();
        Assert.Same(actor, _room.GetRoomUserManager().GetRoomUserByVirtualId(actor.VirtualId));

        actor.IdleTime = ticks;
        ExecutorTick();
        Assert.Null(_client.GetHabbo().CurrentRoom);
        Assert.DoesNotContain(actor, _room.GetRoomUserManager().GetUserList());
    }

    [Fact]
    public void AnIdleKickClosesTheRoomWithReasonTwoAndNoGenericError()
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleAutokickTimeoutSeconds = 60 };
        var actor = ExecutorActor(0, 1);
        _room.OwnerId = 99;
        actor.IdleTime = 120;

        ExecutorTick();

        var headers = _client.Packets.Select(packet => packet.Header).ToList();
        Assert.DoesNotContain(ServerPacketHeader.GenericErrorComposer, headers);
        var playing = headers.IndexOf(ServerPacketHeader.YouArePlayingGameComposer);
        var close = headers.IndexOf(ServerPacketHeader.CloseConnectionComposer);
        Assert.True(playing >= 0 && close > playing, string.Join(",", headers));
        Assert.Equal(new byte[] { 0, 2 }, _client.Packets[close].Body);
    }

    [Fact]
    public void WithIdleAutokickOffAnIdleVisitorStays()
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleAutokickEnabled = false };
        var actor = ExecutorActor(0, 1);
        _room.OwnerId = 99;
        actor.IdleTime = 100_000;

        ExecutorTick();

        Assert.Same(_room, _client.GetHabbo().CurrentRoom);
        Assert.Contains(actor, _room.GetRoomUserManager().GetUserList());
    }

    [Fact]
    public void TheOwnerIsNeverKickedForBeingIdle()
    {
        _room.Behavior = RoomBehaviorSettings.Default with { IdleAutokickTimeoutSeconds = 60 };
        var actor = ExecutorActor(0, 1);
        actor.IdleTime = 100_000;

        ExecutorTick();

        Assert.Contains(actor, _room.GetRoomUserManager().GetUserList());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void MuteAllPetsSilencesEveryPetLine(bool muted, int lines)
    {
        Viewer(0, 0);
        var pet = BuiltinPet(0);
        pet.PetData!.Attach(_room, TestGameClientManager.Empty, null!);
        pet.PetData.Experience = 149_995;
        _room.Behavior = RoomBehaviorSettings.Default with { MuteAllPets = muted };
        _client.Sent.Clear();

        pet.Chat("woof");
        Assert.Equal(lines, _client.Sent.Count(header => header == ServerPacketHeader.ChatComposer));

        pet.PetData.Addexperience(5);
        Assert.Equal(lines * 2, _client.Sent.Count(header => header == ServerPacketHeader.ChatComposer));
    }

    [Fact]
    public void MuteAllPetsKeepsOtherBotsTalking()
    {
        Viewer(0, 0);
        var bot = BuiltinPet(0);
        bot.PetData = null;
        bot.BotData!.AiType = BotAiType.Generic;
        _room.Behavior = RoomBehaviorSettings.Default with { MuteAllPets = true };
        _client.Sent.Clear();

        bot.Chat("hello");

        Assert.Single(_client.Sent, header => header == ServerPacketHeader.ChatComposer);
    }
}
