using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

// The owner's "Breed" command (46) sends the pet to a breeding box for its type. When it cannot go, Habbo
// answers the owner with GoToBreedingNestFailure: 0 no breeding box, 1 no box for this pet type, 5 no way to the box.
public partial class PlacedFurniRoomTests
{
    private const int Breed = 46;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BreedInARoomWithoutABreedingBoxAnswersThatThereIsNoNest(bool normalNest)
    {
        var (owner, dog) = BreedingDog(0, 2);

        if (normalNest) {
            BreedingFurni(70, "nest_plow_red", null, 3, 3);
        }

        Command(dog, owner, Breed);
        ExecutorTick();

        Assert.Equal([0], NestFailures());
        Assert.Equal((92, 120), (dog.PetData!.Experience, dog.PetData.Energy));
    }

    [Fact]
    public void BreedWithOnlyABoxForAnotherPetTypeAnswersThatNoNestSuitsThePet()
    {
        var (owner, dog) = BreedingDog(0, 2);
        BreedingFurni(70, "pet_breeding_cat", 1, 3, 3);

        Command(dog, owner, Breed);
        ExecutorTick();

        Assert.Equal([1], NestFailures());
        ExecutorTick();
        Assert.Equal((0, 2), (dog.X, dog.Y));
    }

    [Fact]
    public void BreedWithNoWayToTheBoxAnswersThatThePetCannotFindAWayAndStays()
    {
        var (owner, dog) = BreedingDog(0, 2);
        BreedingFurni(70, "pet_breeding_dog", 0, 3, 3);
        Blocker(71, 2, 2);
        Blocker(72, 2, 3);
        Blocker(73, 3, 2);

        Command(dog, owner, Breed);
        ExecutorTick();

        Assert.Equal([5], NestFailures());
        ExecutorTick();
        ExecutorTick();
        Assert.Equal((0, 2), (dog.X, dog.Y));
        Assert.Equal((92, 120), (dog.PetData!.Experience, dog.PetData.Energy));
    }

    [Fact]
    public void BreedWithAReachableBoxSendsThePetOntoTheBox()
    {
        var (owner, dog) = BreedingDog(0, 2);
        BreedingFurni(70, "pet_breeding_dog", 0, 3, 3);

        Command(dog, owner, Breed);
        ExecutorTick();
        Assert.Empty(NestFailures());

        for (var tick = 0; tick < 8 && (dog.X, dog.Y) != (3, 3); tick++) {
            ExecutorTick();
        }

        Assert.Equal((3, 3), (dog.X, dog.Y));
        Assert.Equal((92, 120), (dog.PetData!.Experience, dog.PetData.Energy));
    }

    private (RoomUser Owner, RoomUser Dog) BreedingDog(int x, int y)
    {
        var owner = ExecutorActor(0, 0);
        var dog = ExternalLifecycleHorse(x, y);
        var pet = dog.PetData!;
        pet.Type = 0;
        pet.ExperienceLevels = [100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000];
        pet.Experience = 92;
        pet.Energy = 120;
        ExecutorTick();

        return (owner, dog);
    }

    private void BreedingFurni(uint id, string name, int? petType, int x, int y)
    {
        var item = Furni(id, InteractionType.None, WiredBoxType.None);
        item.Definition.ItemName = name;
        item.Definition.BreedingPetType = petType;
        item.Definition.Walkable = true;
        item.Definition.Height = 0.1;
        item.Definition.Width = item.Definition.Length = 1;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, x, y, 0, true, false, false));
    }

    private void Blocker(uint id, int x, int y)
    {
        var item = Furni(id, InteractionType.None, WiredBoxType.None);
        item.Definition.Height = 1;
        item.Definition.Width = item.Definition.Length = 1;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, x, y, 0, true, false, false));
    }

    private int[] NestFailures() => _client.Packets
        .Where(packet => packet.Header == ServerPacketHeader.GoToBreedingNestFailureComposer)
        .Select(packet => BinaryPrimitives.ReadInt32BigEndian(packet.Body))
        .ToArray();
}
