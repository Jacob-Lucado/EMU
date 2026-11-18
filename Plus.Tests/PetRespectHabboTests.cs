using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

// On Habbo a scratch makes the pet 10 points happier, up to the maximum, and the pet shows a joy gesture.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(81, 91)]
    [InlineData(-3, 0)]
    public void AScratchMakesThePetTenPointsHappierUpToTheMaximum(int happiness, int expected)
    {
        LegacyRider();
        var petUser = LegacyHorse(2, 1);
        var pet = Assert.IsType<Pet>(petUser.PetData);
        pet.Nutrition = happiness < 0 ? Pet.MaxNutrition + happiness : happiness;
        _client.GetHabbo().HabboStats = RespectStats(3);

        RespectService((_, _) => { }, (_, _) => { }, () => { }).Respect(_room, _client, pet.PetId);

        Assert.Equal(expected <= 0 ? Pet.MaxNutrition + expected : expected, pet.Nutrition);
    }

    [Fact]
    public void AScratchedPetShowsJoy()
    {
        LegacyRider();
        var petUser = LegacyHorse(2, 1);
        _client.GetHabbo().HabboStats = RespectStats(3);

        RespectService((_, _) => { }, (_, _) => { }, () => { }).Respect(_room, _client, 50);

        Assert.Equal("joy", petUser.Statusses.GetValueOrDefault("gst"));
    }
}
