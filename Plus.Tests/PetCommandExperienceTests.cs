using Xunit;

namespace Plus.Tests;

// Habbo gives each obeyed command its own experience. Down depends on the pet type. A command without a known value gives 5.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(28, 1, 10)]
    [InlineData(8, 2, 10)]
    [InlineData(17, 2, 15)]
    [InlineData(20, 2, 25)]
    [InlineData(15, 44, 5)]
    [InlineData(0, 2, 5)]
    [InlineData(0, 3, 5)]
    public void AnObeyedCommandGivesHabbosExperienceForThePetTypeAndCommand(int type, int command, int experience)
    {
        var owner = LegacyRider();
        var pet = CommandPet(type, 17_600, 300);

        Command(PetUser(pet), owner, command);

        Assert.Equal(17_600 + experience, pet.Experience);
    }
}
