using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.PathFinding;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void SteppingOntoASnowboardSlopeGivesTheSnowboardEffect()
    {
        var (actor, service) = SnowboardRun(0);

        service.Apply(actor, 2, 1, lastStep: false);

        Assert.Equal(97, Assert.IsType<Plus.HabboHotel.Users.Effects.EffectsComponent>(_client.GetHabbo().Effects).CurrentEffect);
        Assert.Equal(ItemEffectType.Snowboard, actor.CurrentItemEffect);
    }

    [Fact]
    public void SteppingOffTheSlopeTakesTheSnowboardAway()
    {
        var (actor, service) = SnowboardRun(0);
        service.Apply(actor, 2, 1, lastStep: false);

        service.Apply(actor, 3, 3);

        Assert.Equal(0, Assert.IsType<Plus.HabboHotel.Users.Effects.EffectsComponent>(_client.GetHabbo().Effects).CurrentEffect);
        Assert.Equal(ItemEffectType.None, actor.CurrentItemEffect);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 9)]
    public void SnowboarderCrossingAJumpDoesAnOllieOrA360(int choice, int expression)
    {
        var (actor, service) = SnowboardRun(choice);
        service.Apply(actor, 2, 1, lastStep: false);
        _client.Packets.Clear();

        service.Apply(actor, 3, 1, lastStep: false);

        Assert.Equal([expression], Expressions());
        Assert.Equal(ItemEffectType.None, actor.CurrentItemEffect);
    }

    [Fact]
    public void SnowboarderStoppingOnAJumpDoesNoTrick()
    {
        var (actor, service) = SnowboardRun(0);
        service.Apply(actor, 2, 1, lastStep: false);
        _client.Packets.Clear();

        service.Apply(actor, 3, 1, lastStep: true);

        Assert.Empty(Expressions());
    }

    [Fact]
    public void WalkerWithoutASnowboardCrossingAJumpDoesNoTrick()
    {
        var (actor, service) = SnowboardRun(0);
        _client.Packets.Clear();

        service.Apply(actor, 3, 1, lastStep: false);

        Assert.Empty(Expressions());
    }

    [Fact]
    public void WalkFromTheSlopeOverAJumpDoesATrickWhenTheStepIsAnnounced()
    {
        IceTagSetup(v2: true);
        Walkable(70, 0, 1, InteractionType.SnowboardSlope, 0, 2);
        Walkable(71, 2, 1, InteractionType.SnowboardJump, 0.3, 1);
        IcePlayer(_client, 0, 1);
        var actions = new RoomAvatarActionService(_interactionClock, Proxy<Plus.HabboHotel.Quests.IQuestManager>((method, _) => throw new InvalidOperationException(method)), TestNavigationRewards.Instance);
        _client.Packets.Clear();

        actions.Move(_client, 3, 1);

        for (var i = 0; i < 12; i++) {
            _room.ProcessRoom();
        }

        Assert.Single(Expressions(), expression => expression is 8 or 9);
    }

    // A slope on (1,1)-(2,2) and a jump on (3,1).
    private (RoomUser Actor, FloorEffectService Service) SnowboardRun(int choice)
    {
        Add(70, 1, 1, type: InteractionType.SnowboardSlope, width: 2, length: 2);
        Add(71, 3, 1, height: 0.3, type: InteractionType.SnowboardJump);
        var actor = LandingActor();

        return (actor, new FloorEffectService(_room, _ => { }, _ => choice));
    }

    private void Walkable(uint id, int x, int y, InteractionType type, double height, int size)
    {
        var item = Furni(id, type, WiredBoxType.None);
        item.Definition.Walkable = true;
        item.Definition.Height = height;
        item.Definition.Width = item.Definition.Length = size;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(_client, item, x, y, 0, true, false, false));
    }

    private int[] Expressions() => _client.Packets.Where(packet => packet.Header == ServerPacketHeader.ActionComposer)
        .Select(packet => BinaryPrimitives.ReadInt32BigEndian(packet.Body.AsSpan(4))).ToArray();
}
