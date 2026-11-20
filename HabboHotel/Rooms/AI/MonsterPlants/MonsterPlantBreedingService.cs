using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

[Singleton]
public interface IMonsterPlantBreedingService
{
    /// <summary>Handles a breed request (state 0), cancel (1) or accept (2) for the user's plant and another plant.</summary>
    void Breed(Room room, GameClient session, int state, int ownPetId, int otherPetId);
}

public sealed class MonsterPlantBreedingService(IMonsterPlantStore store, IMonsterPlantGenetics genetics, IItemDataManager items,
    TimeProvider clock) : IMonsterPlantBreedingService
{
    public void Breed(Room room, GameClient session, int state, int ownPetId, int otherPetId) { }
}
