using Plus.HabboHotel.Users.Effects;
using Xunit;

namespace Plus.Tests;

// An effect a user got in a room ends when the user leaves it, so the next room entry does not bring it back.
// This holds for the effect of a vending machine and for an effect chosen from the effects inventory.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task AVendingEffectEndsWhenTheUserLeavesTheRoom()
    {
        var (clock, manager) = GridRoom();
        ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        RunRoom(manager, clock, usedAt + 1600, period: 25);
        await WalkAway(3, 3);
        RunRoom(manager, clock, clock.Ms + 2000, period: 25);
        Assert.Equal(VendingEffect, effects.CurrentEffect);

        _room.GetRoomUserManager().RemoveUserFromRoom(_client, true, false);

        Assert.Equal(0, effects.CurrentEffect);
    }

    [Fact]
    public void AnEffectChosenFromTheEffectsInventoryEndsWhenTheUserLeavesTheRoom()
    {
        var (clock, _) = GridRoom();
        ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        effects.TryAdd(new AvatarEffect(1, 7, 19, 3600, true, DateTimeOffset.UtcNow, 1));

        new AvatarEffectService(TimeProvider.System).Select(_client, 19);
        Assert.Equal(19, effects.CurrentEffect);
        _room.GetRoomUserManager().RemoveUserFromRoom(_client, true, false);

        Assert.Equal(0, effects.CurrentEffect);
    }

    [Fact]
    public async Task AnEffectThatReplacedTheVendingEffectIsNotRemovedByTheVendingMachine()
    {
        var (clock, manager) = GridRoom();
        var actor = ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        RunRoom(manager, clock, usedAt + 1600, period: 25);
        await WalkAway(3, 3);
        RunRoom(manager, clock, clock.Ms + 2000, period: 25);
        actor.ApplyEffect(12);

        _room.GetRoomUserManager().RemoveUserFromRoom(_client, true, false);

        Assert.Equal(12, effects.CurrentEffect);
    }
}
