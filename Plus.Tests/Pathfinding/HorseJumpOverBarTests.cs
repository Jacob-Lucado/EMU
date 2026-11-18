using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

// Like Habbo, a ridden horse jumps the bar of a horse jump when its rider walks straight along the jump's length.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(2, false)]
    [InlineData(6, true)]
    public void ARiddenHorseJumpsStraightOverTheBar(int rotation, bool layered)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", rotation);
        var (from, to) = rotation is 2 or 6 ? ((0, 1), (4, 1)) : ((1, 0), (1, 4));
        var (rider, horse) = JumpingRider(from, layered);

        rider.MoveTo(to.Item1, to.Item2);
        var trail = FallbackRun(horse, 10);

        Assert.Contains(trail, step => HorseJumpBar(rotation).Contains((step.X, step.Y)));
        Assert.Equal(to, (horse.X, horse.Y));
        Assert.Equal(to, (rider.X, rider.Y));
        Assert.True(rider.RidingHorse);
    }

    [Fact]
    public void TheHorseShowsJmpAndTheRiderRideJumpsOnlyOnTheStepOntoTheBar()
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        var (rider, horse) = JumpingRider((1, 0), false);
        var ticks = new List<(bool Jmp, (int X, int Y)? Target, int[] Expressions)>();

        rider.MoveTo(1, 4);

        for (var tick = 0; tick < 8; tick++) {
            ExecutorTick();
            var target = horse.HasStatus("mv") ? (horse.SetX, horse.SetY) : ((int, int)?)null;
            ticks.Add((horse.HasStatus("jmp"), target, RiderExpressions(rider)));
        }

        var jump = Assert.Single(ticks, tick => tick.Jmp);
        var at = ticks.IndexOf(jump);
        Assert.Equal((1, 2), jump.Target);
        Assert.Equal([10], jump.Expressions);
        Assert.Equal([0], ticks[at + 1].Expressions);
        Assert.All(ticks.Where((_, index) => index != at && index != at + 1), tick => Assert.Empty(tick.Expressions));
    }

    [Theory]
    [InlineData(1, 1, 2, 3)]
    [InlineData(2, 3, 1, 0)]
    [InlineData(1, 0, 2, 4)]
    public void ARiderStepsOntoAndOffTheBarOnlyAlongTheJump(int fromX, int fromY, int toX, int toY)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        var (rider, horse) = JumpingRider((fromX, fromY), false);

        rider.MoveTo(toX, toY);
        var trail = FallbackRun(horse, 10);
        trail.Insert(0, (fromX, fromY, 0));

        for (var i = 1; i < trail.Count; i++) {
            var (a, b) = (trail[i - 1], trail[i]);

            if (HorseJumpBar(4).Contains((a.X, a.Y)) || HorseJumpBar(4).Contains((b.X, b.Y))) {
                Assert.Equal(a.X, b.X);
            }
        }

        Assert.Equal((toX, toY), (horse.X, horse.Y));
    }

    private (RoomUser Rider, RoomUser Horse) JumpingRider((int X, int Y) at, bool layered)
    {
        var rider = FallbackActor(at.X, at.Y, new() { LayeringEnabled = layered });
        var horse = ExternalLifecycleHorse(at.X, at.Y);
        ExecutorTick();
        EstablishExternalMountedGroup(rider, horse);
        ExecutorTick();

        return (rider, horse);
    }

    private int[] RiderExpressions(RoomUser rider) => _client.Packets
        .Where(packet => packet.Header == ServerPacketHeader.ActionComposer)
        .Select(packet => packet.Body.ToArray())
        .Where(body => BinaryPrimitives.ReadInt32BigEndian(body) == rider.VirtualId)
        .Select(body => BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(4)))
        .ToArray();
}
