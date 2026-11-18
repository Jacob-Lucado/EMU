using System.Runtime.CompilerServices;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

// Habbo's pet info lists the horse jump skill levels for horses only, and a normal pet can never be harvested.
public sealed class PetInfoHabboFieldTests
{
    [Fact]
    public void AHorseListsItsJumpSkillLevels()
    {
        var writes = Writes(Pet(15));

        Assert.Equal(new object[] { 5, 0, 2, 6, 10, 14 }, writes[17..23]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void OtherPetsListNoSkillLevels(int type)
    {
        var writes = Writes(Pet(type));

        Assert.Equal(0, writes[17]);
    }

    [Fact]
    public void ANormalPetCannotBeHarvested()
    {
        var writes = Writes(Pet(0));

        Assert.Equal(new object[] { false, false, false }, writes[19..22]);
    }

    private static Pet Pet(int type)
    {
        var pet = (Pet)RuntimeHelpers.GetUninitializedObject(typeof(Pet));
        pet.PetId = 42;
        pet.Name = "Gallopy";
        pet.OwnerName = "Objecto";
        pet.Type = type;
        pet.ExperienceLevels = [100, 200];

        return pet;
    }

    private static object[] Writes(Pet pet)
    {
        var packet = new HabbiconTestSupport.RecordingPacket();
        new PetInformationComposer(new PetInformationService(TimeProvider.System).Capture(pet)).Compose(packet);

        return packet.Writes.ToArray();
    }
}
