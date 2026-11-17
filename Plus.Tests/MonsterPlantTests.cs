using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantTests
{
    private static readonly DateTimeOffset PlantedAt = DateTimeOffset.Parse("2040-01-01T00:00:00Z");

    [Fact]
    public void ASeedlingStartsWith36HoursOfWellbeing()
    {
        var plant = MonsterPlant.Seeded(1, 0, PlantedAt);

        Assert.Equal(PlantedAt.AddSeconds(129600), plant.WellbeingUntil);
        Assert.Equal(TimeSpan.FromSeconds(129600), plant.MaxWellbeing(PlantedAt));
    }

    [Fact]
    public void WellbeingCountsDownToZeroAndThenThePlantIsDead()
    {
        var plant = MonsterPlant.Seeded(1, 0, PlantedAt);

        Assert.Equal(TimeSpan.FromSeconds(129591), plant.RemainingWellbeing(PlantedAt.AddSeconds(9)));
        Assert.False(plant.IsDead(PlantedAt.AddSeconds(129599)));
        Assert.True(plant.IsDead(PlantedAt.AddSeconds(129600)));
        Assert.Equal(TimeSpan.Zero, plant.RemainingWellbeing(PlantedAt.AddDays(4)));
    }

    [Fact]
    public void ADeadPlantShowsTheRipPosture()
    {
        Assert.Equal("rip", MonsterPlant.Seeded(1, 0, PlantedAt).Posture(PlantedAt.AddDays(5)));
    }

    [Theory]
    [InlineData(0, 1, 0, 20, "grw1")]
    [InlineData(28799, 1, 19, 20, "grw1")]
    [InlineData(28800, 2, 20, 40, "grw2")]
    [InlineData(144000, 6, 100, 120, "grw6")]
    [InlineData(172799, 6, 119, 120, "grw6")]
    [InlineData(172800, 7, 120, 120, "std")]
    [InlineData(400000, 7, 120, 120, "std")]
    public void APlantGrowsOneLevelEveryEightHoursForTwoDays(int seconds, int level, int experience, int goal, string posture)
    {
        var now = PlantedAt.AddSeconds(seconds);
        var plant = new MonsterPlant(1, 0, PlantedAt, now.AddHours(1), true, false);

        Assert.Equal((level, experience, goal, posture), (plant.Level(now), plant.Experience(now), plant.ExperienceGoal(now), plant.Posture(now)));
        Assert.Equal(TimeSpan.FromSeconds(Math.Max(0, 172800 - seconds)), plant.RemainingGrowth(now));
    }

    [Fact]
    public void AGrownPlantHoldsThreeDaysOfWellbeing()
    {
        var now = PlantedAt.AddDays(10);
        var plant = new MonsterPlant(1, 4, PlantedAt, now.AddSeconds(249350), false, true);

        Assert.Equal(TimeSpan.FromSeconds(259200), plant.MaxWellbeing(now));
        Assert.Equal(115, plant.Energy(now));
    }

    [Fact]
    public void EnergyIsWellbeingOutOf120RoundedDown()
    {
        var plant = MonsterPlant.Seeded(1, 0, PlantedAt);

        Assert.Equal(119, plant.Energy(PlantedAt.AddSeconds(9)));
        Assert.Equal(120, plant.Energy(PlantedAt));
        Assert.Equal(0, plant.Energy(PlantedAt.AddDays(3)));
    }

    [Fact]
    public void TreatingALivingPlantFillsItsWellbeing()
    {
        var now = PlantedAt.AddDays(10);
        var plant = new MonsterPlant(1, 4, PlantedAt, now.AddSeconds(249350), false, true);

        Assert.True(plant.Treat(now));

        Assert.Equal(TimeSpan.FromSeconds(259200), plant.RemainingWellbeing(now));
        Assert.Equal(120, plant.Energy(now));
    }

    [Fact]
    public void ADeadPlantCannotBeTreated()
    {
        var plant = MonsterPlant.Seeded(1, 0, PlantedAt);
        var later = PlantedAt.AddDays(5);

        Assert.False(plant.Treat(later));
        Assert.True(plant.IsDead(later));
    }

    [Fact]
    public void OnlyAGrownLivingPlantThatHasNotBredCanBreed()
    {
        var grown = PlantedAt.AddDays(2);

        Assert.False(new MonsterPlant(1, 0, PlantedAt, grown.AddDays(1), true, false).CanBreedNow(grown.AddSeconds(-1)));
        Assert.True(new MonsterPlant(1, 0, PlantedAt, grown.AddDays(1), true, false).CanBreedNow(grown));
        Assert.False(new MonsterPlant(1, 0, PlantedAt, grown.AddDays(1), false, false).CanBreedNow(grown));
        Assert.False(new MonsterPlant(1, 0, PlantedAt, grown, true, false).CanBreedNow(grown));
    }
}
