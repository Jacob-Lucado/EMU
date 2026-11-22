using Xunit;

namespace Plus.Tests;

// Every 2x3 horse obstacle is a horse jump: hedges, the ring of fire and the water jumps as well as the oxers.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData("obstacle_bush", false)]
    [InlineData("obstacle_bush", true)]
    [InlineData("obstacle_fring", false)]
    [InlineData("obstacle_fring", true)]
    [InlineData("obstacle_water", false)]
    [InlineData("obstacle_water", true)]
    [InlineData("obstacle_water2", false)]
    [InlineData("obstacle_water2", true)]
    [InlineData("obstacle_water3", false)]
    [InlineData("obstacle_water3", true)]
    public void AUserWalksAroundTheBarOfEveryHorseObstacle(string name, bool layered)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, name, 4, height: 0);
        var actor = FallbackActor(1, 0, new() { LayeringEnabled = layered });

        actor.MoveTo(1, 4);
        var trail = FallbackRun(actor, 12);

        Assert.DoesNotContain(trail, step => HorseJumpBar(4).Contains((step.X, step.Y)));
        FallbackAssertStopped(actor, 1, 4);
    }

    [Theory]
    [InlineData("obstacle_bush", false)]
    [InlineData("obstacle_bush", true)]
    [InlineData("obstacle_fring", false)]
    [InlineData("obstacle_fring", true)]
    [InlineData("obstacle_water", false)]
    [InlineData("obstacle_water", true)]
    [InlineData("obstacle_water2", false)]
    [InlineData("obstacle_water2", true)]
    [InlineData("obstacle_water3", false)]
    [InlineData("obstacle_water3", true)]
    public void ARiddenHorseJumpsEveryHorseObstacle(string name, bool layered)
    {
        FallbackModel(HorseJumpRoom);
        HorseJump(20, name, 4, height: 0);
        var (rider, horse) = JumpingRider((1, 0), layered);

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
