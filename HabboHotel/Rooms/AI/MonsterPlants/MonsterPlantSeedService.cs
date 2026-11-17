using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>The figure parts of a plant pet: <c>16 race color parts</c>.</summary>
public sealed record MonsterPlantLook(int Race, string Color, string Parts);

/// <summary>What a seed grows into.</summary>
public sealed record MonsterPlantSeedling(string Name, MonsterPlantLook Look, int Rarity);

[Singleton]
public interface IMonsterPlantGenetics
{
    MonsterPlantSeedling Roll(ItemDefinition seed);
}

[Singleton]
public interface IMonsterPlantSeedService
{
    /// <summary>Plants the room item when it is a seed. Returns false when the item is not a seed.</summary>
    bool TryPlant(Room room, GameClient session, uint itemId);
}

public sealed class MonsterPlantSeedService(IMonsterPlantStore store, IMonsterPlantGenetics genetics, TimeProvider clock)
    : IMonsterPlantSeedService
{
    public bool TryPlant(Room room, GameClient session, uint itemId) => throw new NotImplementedException();
}
