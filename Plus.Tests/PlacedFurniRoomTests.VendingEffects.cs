using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Effects;
using Xunit;

namespace Plus.Tests;

// A vending machine with an effect gives the effect on the avatar's first step 1250 ms or more after the use.
// A standing avatar gets nothing. An avatar that already has the effect gets no packet.
public partial class PlacedFurniRoomTests
{
    private const int VendingEffect = 164;

    [Fact]
    public async Task AVendingEffectComesOnTheFirstStepAfter1250MsAfterTheUse()
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
        var passes = RunRoom(manager, clock, clock.Ms + 2000, period: 25);

        var effect = Assert.Single(passes, pass => EffectsSent(pass.Packets).Any());
        Assert.Equal([VendingEffect], EffectsSent(effect.Packets));
        Assert.True(HasUserUpdate(effect));
        Assert.Equal((3, 3), (actor.X, actor.Y));
        Assert.Equal(VendingEffect, effects.CurrentEffect);
    }

    [Fact]
    public async Task AVendingEffectWaitsForTheFirstStepWhenTheAvatarStandsStill()
    {
        var (clock, manager) = GridRoom();
        var actor = ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        var standing = RunRoom(manager, clock, usedAt + 6000, period: 25);
        Assert.DoesNotContain(standing, pass => EffectsSent(pass.Packets).Any());
        Assert.Equal(0, effects.CurrentEffect);

        await WalkAway(3, 3);
        var walking = RunRoom(manager, clock, clock.Ms + 2000, period: 25);

        var effect = Assert.Single(walking, pass => EffectsSent(pass.Packets).Any());
        Assert.True(HasUserUpdate(effect));
        Assert.Equal((3, 3), (actor.X, actor.Y));
        Assert.Equal(VendingEffect, effects.CurrentEffect);
    }

    [Fact]
    public async Task AVendingEffectIsNotSentAgainWhenTheAvatarAlreadyHasIt()
    {
        var (clock, manager) = GridRoom();
        var actor = ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        effects.ApplyEffect(VendingEffect);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        RunRoom(manager, clock, usedAt + 1600, period: 25);
        await WalkAway(3, 3);
        var passes = RunRoom(manager, clock, clock.Ms + 2000, period: 25);

        Assert.Equal((3, 3), (actor.X, actor.Y));
        Assert.DoesNotContain(passes, pass => EffectsSent(pass.Packets).Any());
        Assert.Equal(VendingEffect, effects.CurrentEffect);
    }

    [Fact]
    public async Task AVendingEffectReplacesADifferentEffect()
    {
        var (clock, manager) = GridRoom();
        var actor = ExecutorActor(2, 1);
        var effects = AvatarEffects(clock);
        effects.ApplyEffect(162);
        var vending = EffectVendingMachine(VendingEffect);
        clock.Ms += 130;
        var usedAt = clock.Ms;

        await UseVending(vending);
        RunRoom(manager, clock, usedAt + 1600, period: 25);
        await WalkAway(3, 3);
        var passes = RunRoom(manager, clock, clock.Ms + 2000, period: 25);

        Assert.Equal((3, 3), (actor.X, actor.Y));
        Assert.Equal([VendingEffect], passes.SelectMany(pass => EffectsSent(pass.Packets)));
        Assert.Equal(VendingEffect, effects.CurrentEffect);
    }

    private EffectsComponent AvatarEffects(TimeProvider clock)
    {
        var effects = new EffectsComponent(clock);
        effects.Init(_client.GetHabbo());
        _client.GetHabbo().Effects = effects;

        return effects;
    }

    private Item EffectVendingMachine(int effectId)
    {
        var vending = VendingMachine();
        vending.Definition.EffectId = effectId;

        return vending;
    }

    private async Task WalkAway(int x, int y)
        => await new MoveAvatarEvent(new RoomAvatarActionService(TimeProvider.System, null!, null!)).Parse(_client, ClientPacket(x, y));

    private static int[] EffectsSent(IEnumerable<(uint Header, byte[] Body)> packets) => [.. packets
        .Where(packet => packet.Header == ServerPacketHeader.AvatarEffectComposer)
        .Select(packet => new FlashIncomingPacket { Buffer = packet.Body })
        .Select(body => { body.ReadInt(); return body.ReadInt(); })];
}
