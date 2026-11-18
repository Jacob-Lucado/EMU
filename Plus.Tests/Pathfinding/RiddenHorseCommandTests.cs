using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

// Habbo ignores Free and commands outside the horse's list while it is ridden. Any other horse command first puts
// the rider down, and then the horse handles it as usual.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public void FreeLeavesTheRiderOnTheHorse()
    {
        var (rider, horse, pet) = RiddenCommandHorse(17_600);

        Command(horse, rider, 0);
        RiddenCommandTicks();

        Assert.True(rider.RidingHorse);
        Assert.True(horse.RidingHorse);
        Assert.Equal((2, 1), (horse.X, horse.Y));
        Assert.Equal((2, 1, 1d), (rider.X, rider.Y, rider.Z));
        Assert.Equal(17_600, pet.Experience);
    }

    [Fact]
    public void ACommandOutsideTheHorsesListLeavesTheRiderOnTheHorse()
    {
        var (rider, horse, _) = RiddenCommandHorse(17_600);

        Command(horse, rider, 1);
        RiddenCommandTicks();

        Assert.True(rider.RidingHorse);
        Assert.True(horse.RidingHorse);
        Assert.DoesNotContain("sit", horse.Statusses.Keys);
    }

    [Theory]
    [InlineData(44, "wag")]
    [InlineData(2, "lay")]
    public void AHorseCommandPutsTheRiderDownBeforeTheHorseObeys(int command, string posture)
    {
        var (rider, horse, pet) = RiddenCommandHorse(17_600);

        Command(horse, rider, command);
        RiddenCommandTicks();

        Assert.False(rider.RidingHorse);
        Assert.False(horse.RidingHorse);
        Assert.Equal((0, 0), (rider.HorseId, horse.HorseId));
        Assert.Equal(0d, rider.Z);
        Assert.Contains(posture, horse.Statusses.Keys);
        Assert.Equal(17_605, pet.Experience);
    }

    [Fact]
    public void HereMovesTheHorseOnlyAfterTheRiderIsDown()
    {
        var (rider, horse, _) = RiddenCommandHorse(17_600);

        Command(horse, rider, 3);
        RiddenCommandTicks();

        Assert.False(rider.RidingHorse);
        Assert.False(horse.RidingHorse);
        Assert.Equal(0d, rider.Z);
    }

    [Fact]
    public void AnUnlearnedHorseCommandPutsTheRiderDownAndPuzzlesTheHorse()
    {
        var (rider, horse, pet) = RiddenCommandHorse(0);

        Command(horse, rider, 2);
        RiddenCommandTicks();

        Assert.False(rider.RidingHorse);
        Assert.False(horse.RidingHorse);
        Assert.Equal("puz", horse.Statusses.GetValueOrDefault("gst"));
        Assert.Equal(0, pet.Experience);
    }

    [Fact]
    public void ACommandAfterAMountRequestDismountsTheNewRider()
    {
        var rider = ExecutorActor(1, 1);
        var horse = ExternalLifecycleHorse(2, 1);
        horse.PetData!.Type = HorseProducts.HorseType;
        ExecutorTick();

        RideExternalHorse(horse, mount: true);
        Command(horse, rider, 2);
        ExecutorTick();

        Assert.False(rider.RidingHorse);
        Assert.False(horse.RidingHorse);
        Assert.Equal("puz", horse.Statusses.GetValueOrDefault("gst"));
    }

    private (RoomUser Rider, RoomUser Horse, Pet Pet) RiddenCommandHorse(int experience)
    {
        var rider = ExecutorActor(1, 1);
        var horse = ExternalLifecycleHorse(2, 1);
        var pet = Assert.IsType<Pet>(horse.PetData);
        pet.Type = HorseProducts.HorseType;
        pet.ExperienceLevels = [100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000];
        pet.Experience = experience;
        pet.Energy = 300;
        ExecutorTick();
        RideExternalHorse(horse, mount: true);
        ExecutorTick();
        Assert.True(rider.RidingHorse);
        Assert.Equal((2, 1, 1d), (rider.X, rider.Y, rider.Z));

        return (rider, horse, pet);
    }

    private void RiddenCommandTicks()
    {
        for (var tick = 0; tick < 8; tick++) {
            ExecutorTick();
        }
    }
}
