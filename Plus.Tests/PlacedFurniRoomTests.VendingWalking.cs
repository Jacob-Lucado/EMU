using Plus.Communication.Packets.Outgoing;
using Xunit;

namespace Plus.Tests;

// A user may walk away at once after using a vending machine. The machine does not hold the user: the hand item and the effect still arrive.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task AUserCanWalkAtOnceAfterUsingAVendingMachine()
    {
        var (clock, manager) = GridRoom();
        var vending = VendingMachine(148);
        var actor = ExecutorActor(2, 1);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        Assert.True(actor.CanWalk);
        await WalkAway(3, 3);
        RunRoom(manager, clock, usedAt + 800, period: 25);

        Assert.NotEqual((2, 1), (actor.X, actor.Y));
    }

    [Fact]
    public async Task AVendingMachineStillHandsItsItemToAUserWhoWalkedAway()
    {
        var (clock, manager) = GridRoom();
        var vending = VendingMachine(148);
        var actor = ExecutorActor(2, 1);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        await WalkAway(3, 3);
        var passes = RunRoom(manager, clock, usedAt + 2500, period: 25);

        var carry = Assert.Single(passes, pass => pass.Packets.Any(packet => packet.Header == ServerPacketHeader.CarryObjectComposer));
        Assert.InRange(carry.At - usedAt, 1250, 1299);
        var reset = Assert.Single(passes, pass => VendingStates(pass.Packets).Contains("0"));
        Assert.InRange(reset.At - usedAt, 1500, 1549);
        Assert.Equal(148, actor.CarryItemId);
        Assert.True(actor.CanWalk);
        Assert.Equal((3, 3), (actor.X, actor.Y));
    }

    [Fact]
    public async Task AVendingEffectComesOnTheFirstStepAfter1250MsWhenTheUserWalksAtOnce()
    {
        var (clock, manager) = GridRoom();
        var actor = ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        await WalkAway(3, 3);
        RunRoom(manager, clock, usedAt + 1100, period: 25);
        Assert.Equal((3, 3), (actor.X, actor.Y));
        await WalkAway(0, 3);
        var passes = RunRoom(manager, clock, usedAt + 3000, period: 25);

        var effect = Assert.Single(passes, pass => EffectsSent(pass.Packets).Any());
        Assert.Equal([VendingEffect], EffectsSent(effect.Packets));
        Assert.InRange(effect.At - usedAt, 1250, 2200);
        Assert.True(HasUserUpdate(effect));
        Assert.Equal(VendingEffect, effects.CurrentEffect);
    }
}
