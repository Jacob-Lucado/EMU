using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

// A horse jump is 2x3 and placed at (1,1). Its middle row across the 3-tile length is the bar.
public partial class PlacedFurniRoomTests
{
    private const string HorseJumpRoom = "00000\r00000\r00000\r00000\r00000";

    [Theory]
    [InlineData("obstacle_oxer", 4, false)]
    [InlineData("obstacle_oxer", 4, true)]
    [InlineData("obstacle_oxer2", 0, false)]
    [InlineData("obstacle_oxer3", 2, true)]
    [InlineData("obstacle_oxer", 6, false)]
    public void AUserCrossesAHorseJumpOnAnEndRowAndNeverOnTheBar(string name, int rotation, bool layered)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, name, rotation);
        var (from, to) = HorseJumpCrossing(rotation);
        var actor = FallbackActor(from.X, from.Y, new() { LayeringEnabled = layered });

        actor.MoveTo(to.X, to.Y);
        var trail = FallbackRun(actor, 12);

        AssertCrossedOnAnEndRow(trail, rotation);
        FallbackAssertStopped(actor, to.X, to.Y);
    }

    [Fact]
    public void ARiddenHorseCrossesAHorseJumpOnAnEndRowAndNeverOnTheBar()
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        var (rider, horse) = HorseJumpRider();

        rider.MoveTo(3, 2);
        var trail = FallbackRun(horse, 12);

        AssertCrossedOnAnEndRow(trail, 4);
        Assert.Equal((3, 2), (horse.X, horse.Y));
        Assert.Equal((3, 2), (rider.X, rider.Y));
    }

    [Fact]
    public void AHorseWalkingAloneCrossesAHorseJumpOnAnEndRowAndNeverOnTheBar()
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        ExecutorActor(4, 4);
        var horse = ExternalLifecycleHorse(0, 2);
        ExecutorTick();

        horse.MoveTo(3, 2);
        var trail = FallbackRun(horse, 12);

        AssertCrossedOnAnEndRow(trail, 4);
        FallbackAssertStopped(horse, 3, 2);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task ARiderClickOnTheBarDoesNotMove(int x, int y)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        var (rider, horse) = HorseJumpRider();

        await new MoveAvatarEvent(new RoomAvatarActionService(TimeProvider.System, null!, null!)).Parse(_client, ClientPacket(x, y));
        FallbackRun(horse, 6);

        FallbackAssertStopped(rider, 0, 2);
        FallbackAssertStopped(horse, 0, 2);
    }

    [Fact]
    public async Task ARiderClickOnAnEndRowWalksOntoItAtTheJumpHeight()
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, "obstacle_oxer", 4);
        var (rider, horse) = HorseJumpRider();

        await new MoveAvatarEvent(new RoomAvatarActionService(TimeProvider.System, null!, null!)).Parse(_client, ClientPacket(1, 3));
        FallbackRun(horse, 6);

        Assert.Equal((1, 3, 0.1), (horse.X, horse.Y, Math.Round(horse.Z, 3)));
        Assert.Equal((1, 3), (rider.X, rider.Y));
    }

    private Item HorseJump(uint id, string name, int rotation, double height = 0.1)
    {
        var jump = Furni(id, InteractionType.None, WiredBoxType.None);
        jump.UserId = 7;
        jump.Definition.ItemName = name;
        jump.Definition.Width = 2;
        jump.Definition.Length = 3;
        jump.Definition.Height = height;
        jump.Definition.Stackable = false;
        jump.Definition.Walkable = true;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, jump, 1, 1, rotation, true, false, false));

        return jump;
    }

    private (RoomUser Rider, RoomUser Horse) HorseJumpRider()
    {
        var rider = ExecutorActor(0, 2);
        var horse = ExternalLifecycleHorse(0, 2);
        ExecutorTick();
        EstablishExternalMountedGroup(rider, horse);

        return (rider, horse);
    }

    private static ((int X, int Y) From, (int X, int Y) To) HorseJumpCrossing(int rotation)
        => rotation is 2 or 6 ? ((2, 0), (2, 3)) : ((0, 2), (3, 2));

    private static (int X, int Y)[] HorseJumpBar(int rotation)
        => rotation is 2 or 6 ? [(2, 1), (2, 2)] : [(1, 2), (2, 2)];

    private static (int X, int Y)[] HorseJumpEndRows(int rotation)
        => rotation is 2 or 6 ? [(1, 1), (1, 2), (3, 1), (3, 2)] : [(1, 1), (2, 1), (1, 3), (2, 3)];

    private static void AssertCrossedOnAnEndRow(List<(int X, int Y, double Z)> trail, int rotation)
    {
        Assert.DoesNotContain(trail, step => HorseJumpBar(rotation).Contains((step.X, step.Y)));
        Assert.Contains(trail, step => HorseJumpEndRows(rotation).Contains((step.X, step.Y)) && Math.Abs(step.Z - 0.1) < 0.001);
    }
}
