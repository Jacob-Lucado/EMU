using Xunit;

namespace Plus.Tests;

// Every 2x3 horse obstacle is a horse jump: hedges, the ring of fire and the water jumps as well as the oxers.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData("obstacle_bush")]
    [InlineData("obstacle_fring")]
    [InlineData("obstacle_water")]
    [InlineData("obstacle_water2")]
    [InlineData("obstacle_water3")]
    public void AUserWalksAroundTheBarOfEveryHorseObstacle(string name)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, name, 4);
        var actor = FallbackActor(1, 0);

        actor.MoveTo(1, 4);
        var trail = FallbackRun(actor, 12);

        Assert.DoesNotContain(trail, step => HorseJumpBar(4).Contains((step.X, step.Y)));
        FallbackAssertStopped(actor, 1, 4);
    }

    [Theory]
    [InlineData("obstacle_bush")]
    [InlineData("obstacle_fring")]
    [InlineData("obstacle_water")]
    [InlineData("obstacle_water2")]
    [InlineData("obstacle_water3")]
    public void ARiddenHorseJumpsEveryHorseObstacle(string name)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, name, 4);
        var (rider, horse) = JumpingRider((1, 0), false);

        rider.MoveTo(1, 4);
        var jumped = false;

        for (var tick = 0; tick < 10; tick++) {
            ExecutorTick();
            jumped |= horse.HasStatus("jmp");
        }

        Assert.True(jumped);
        Assert.Equal((1, 4), (horse.X, horse.Y));
    }
}
