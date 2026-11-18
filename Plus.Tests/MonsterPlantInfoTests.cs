using System.Runtime.CompilerServices;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantInfoTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(2_200_000_000);

    [Fact]
    public void ADeadGrownPlantShowsItsInfoLikeHabbo()
    {
        var pet = Plant(new MonsterPlant(42, 2, Now.AddDays(-30), Now.AddDays(-1), false, false));
        pet.Respect = 5;

        var writes = Writes(pet);

        Assert.Equal(new object[] { 42, "Aenueus Weggytum", 7, 7, 120, 120, 0, 120, 100, 100, 5, 7, 30, "Owner", 0,
            false, false, 0, 0, false, false, true, 2, 259200, 0, 0, false }, writes);
    }

    [Fact]
    public void APlantInTheInventoryShowsItsGrowthLevelAndRarity()
    {
        var pet = Plant(new MonsterPlant(42, 5, Now.AddHours(-17), Now.AddHours(10), true, false));
        pet.Color = "ffffff";
        pet.GnomeClothing = "2 0 -1 6 1 5 0";
        var packet = new HabbiconTestSupport.RecordingPacket();

        new Plus.Communication.Packets.Outgoing.Inventory.Pets.PetInventoryComposer(
            PetAppearanceSnapshots.Inventory([pet], new FixedClock(Now))).Compose(packet);

        Assert.Equal(new object[] { 3, 5 }, packet.Writes.TakeLast(2));
    }

    private static Pet Plant(MonsterPlant plant)
    {
        var pet = (Pet)RuntimeHelpers.GetUninitializedObject(typeof(Pet));
        pet.PetId = 42;
        pet.Name = "Aenueus Weggytum";
        pet.Type = MonsterPlant.PetType;
        pet.Race = "0";
        pet.OwnerId = 7;
        pet.OwnerName = "Owner";
        pet.CreatedAt = Now.AddDays(-30);
        pet.ExperienceLevels = [100];
        pet.Plant = plant;

        return pet;
    }

    private static object[] Writes(Pet pet)
    {
        var packet = new HabbiconTestSupport.RecordingPacket();
        new PetInformationComposer(new PetInformationService(new FixedClock(Now)).Capture(pet)).Compose(packet);

        return packet.Writes.ToArray();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
