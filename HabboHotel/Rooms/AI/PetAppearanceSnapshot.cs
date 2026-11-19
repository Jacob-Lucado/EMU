using System.Collections.Immutable;
using System.Globalization;

namespace Plus.HabboHotel.Rooms.AI;

public sealed record PetInventoryEntry(int Id, string Name, int Type, int Race, string Color, ImmutableArray<int> CustomParts, int Level, int Rarity = -1);
public sealed record PetInventorySnapshot(ImmutableArray<PetInventoryEntry> Pets);
public sealed record HorseAppearanceSnapshot(int VirtualId, int PetId, int Type, int Race, string Color, int Hair, int HairDye, int Saddle, bool Riding)
{
    public int Breed => HorseProducts.Breed(Race);
}

public static class PetAppearanceSnapshots
{
    public static PetInventorySnapshot Inventory(IEnumerable<Pet> pets, TimeProvider? clock = null) => new(pets.Select(pet => new PetInventoryEntry(
        pet.PetId, pet.Name, pet.Type, int.Parse(pet.Race, CultureInfo.InvariantCulture), pet.Color,
        CustomParts(pet),
        pet.Plant?.Level((clock ?? TimeProvider.System).GetUtcNow()) ?? pet.Level, pet.Plant?.Rarity ?? -1)).ToImmutableArray());

    // The parts start with their count, so a pet without parts sends a count of 0.
    private static ImmutableArray<int> CustomParts(Pet pet) => pet.CustomParts.Length == 0
        ? [0]
        : pet.CustomParts.Split(' ').Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToImmutableArray();

    public static HorseAppearanceSnapshot Horse(RoomUser user)
    {
        var pet = user.PetData ?? throw new InvalidOperationException("Only a placed pet has a horse appearance.");

        return new(pet.VirtualId, pet.PetId, pet.Type, int.Parse(pet.Race, CultureInfo.InvariantCulture),
            pet.Color.ToLowerInvariant(), pet.PetHair, HorseProducts.HairPalette(pet.HairDye), pet.Saddle, user.RidingHorse);
    }
}
