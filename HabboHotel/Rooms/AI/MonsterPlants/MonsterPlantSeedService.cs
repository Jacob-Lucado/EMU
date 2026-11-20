using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
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
    /// <summary>Rolls a look. A seed with a rarity grows into a plant of exactly that rarity.</summary>
    MonsterPlantSeedling Roll(ItemDefinition seed, int? rarity);
}

[Singleton]
public interface IMonsterPlantSeedService
{
    /// <summary>Plants the room item when it is a seed. Returns false when the item is not a seed.</summary>
    bool TryPlant(Room room, GameClient session, uint itemId);
}

public sealed class MonsterPlantSeedService(IMonsterPlantStore store, IMonsterPlantGenetics genetics, TimeProvider clock,
    IRewardTrackManager rewards)
    : IMonsterPlantSeedService
{
    public bool TryPlant(Room room, GameClient session, uint itemId)
    {
        var item = room.GetRoomItemHandler().GetItem(itemId);

        if (item?.Definition?.SpecialType != (int)FurniCategory.MonsterplantSeed) {
            return false;
        }

        var habbo = session.GetHabbo();

        lock (item) {
            if (!ReferenceEquals(habbo.CurrentRoom, room)
                || !ReferenceEquals(room.GetRoomItemHandler().GetItem(itemId), item)
                || item.IsTemporary || item.RoomId != room.RoomId || item.OwnerId != habbo.Id) {
                return true;
            }

            var seedling = genetics.Roll(item.Definition, MonsterPlantSeedRarity.Of(item.ExtraData));
            var pet = store.Plant(new(item.Id, item.Definition.Id, habbo.Id, habbo.Username, room.RoomId,
                item.GetX, item.GetY, item.GetZ, seedling.Name, seedling.Look, seedling.Rarity, clock.GetUtcNow()));

            if (pet == null) {
                return true;
            }

            room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
            rewards.Progress(session, RewardTrackActions.PlantMonsterPlant);
            var speeches = new List<RandomSpeech>();
            room.GetRoomUserManager().DeployBot(new(pet.PetId, pet.RoomId, "pet", "stand", pet.Name, "", pet.Look,
                pet.X, pet.Y, pet.Z, 0, 0, 0, 0, 0, ref speeches, "", 0, pet.OwnerId, false, 0, false, 0), pet);
        }

        return true;
    }
}
