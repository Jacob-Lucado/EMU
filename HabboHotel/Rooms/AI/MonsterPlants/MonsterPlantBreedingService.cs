using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

[Singleton]
public interface IMonsterPlantBreedingService
{
    /// <summary>Handles a breed request (state 0), cancel (1) or accept (2) for the user's plant and another plant.</summary>
    void Breed(Room room, GameClient session, int state, int ownPetId, int otherPetId);
}

/// <summary>
/// Two grown plants of the same owner breed at once into one seed for the owner. Both plants live on but cannot breed
/// again. Breeding with another owner's plant is not supported.
/// </summary>
public sealed class MonsterPlantBreedingService(IMonsterPlantStore store, IMonsterPlantGenetics genetics, IItemDataManager items,
    TimeProvider clock) : IMonsterPlantBreedingService
{
    private const int RequestState = 0;
    private const string SeedItemName = "mnstr_seed";
    private const string BredLine = "Now I know how Pete Rose felt.";

    public void Breed(Room room, GameClient session, int state, int ownPetId, int otherPetId)
    {
        var habbo = session.GetHabbo();
        var users = room.GetRoomUserManager();

        if (state != RequestState || ownPetId == otherPetId || habbo == null || !ReferenceEquals(habbo.CurrentRoom, room)
            || !users.TryGetPet(ownPetId, out var own) || !users.TryGetPet(otherPetId, out var other)
            || own.PetData is not { Plant: { } ownPlant } ownPet || other.PetData is not { Plant: { } otherPlant } otherPet
            || ownPet.OwnerId != habbo.Id || otherPet.OwnerId != habbo.Id) {
            return;
        }

        var now = clock.GetUtcNow();

        if (ownPlant.AfterBreeding(now) is not { } ownBred || otherPlant.AfterBreeding(now) is not { } otherBred
            || items.GetItemByName(SeedItemName) is not { } definition) {
            return;
        }

        var rarity = genetics.BreedRarity(ownPlant.Rarity, otherPlant.Rarity);

        if (store.Breed(ownBred, otherBred, definition, habbo, rarity) is not { } seed) {
            return;
        }

        ownPet.Plant = ownBred;
        otherPet.Plant = otherBred;
        var inventorySeed = seed.ToInventoryItem();

        if (habbo.Inventory?.Furniture.AddItem(inventorySeed) == true) {
            session.Send(new FurniListNotificationComposer(seed.Id, 1));
            session.Send(new FurniListAddComposer(InventoryItemSnapshot.Capture(inventorySeed)));
        }

        (own.BotAi as MonsterPlantBot)?.Announce();
        (other.BotAi as MonsterPlantBot)?.Announce();
        session.Send(new PetBreedingResultComposer(
            new((int)seed.Id, definition.SpriteId, definition.ItemName, habbo.Id, habbo.Username, rarity, false), PetBreedingResultSeed.None));
        (own.BotAi as MonsterPlantBot)?.Gesture("rev");

        if (!room.Behavior.MuteAllPets) {
            room.SendChat(receiver => new ChatComposer(own.VirtualId, BredLine, 0, 0, receiver));
        }
    }
}
