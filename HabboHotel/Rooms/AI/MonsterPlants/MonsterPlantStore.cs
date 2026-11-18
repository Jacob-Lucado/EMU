using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

public sealed record MonsterPlantSeedRequest(uint ItemId, uint BaseItem, int OwnerId, string OwnerName, uint RoomId,
    int X, int Y, double Z, string Name, MonsterPlantLook Look, int Rarity, DateTimeOffset PlantedAt);

[Singleton]
public interface IMonsterPlantStore
{
    /// <summary>Turns a placed seed into a plant pet: the seed row goes and the pet rows come in one transaction.</summary>
    Pet? Plant(MonsterPlantSeedRequest request);

    void Save(MonsterPlant plant);
}

public sealed class MonsterPlantStore(IDatabase database, ILogger<MonsterPlantStore> logger) : IMonsterPlantStore
{
    public Pet? Plant(MonsterPlantSeedRequest request)
    {
        try {
            using var connection = database.Connection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var pet = PetUtility.CreatePet(connection, transaction, request.PlantedAt, request.OwnerName,
                request.OwnerId, request.Name, MonsterPlant.PetType, request.Look.Race.ToString(), request.Look.Color,
                new(request.ItemId, request.BaseItem, request.RoomId, request.X, request.Y, request.Z), request.Look.Parts);

            if (pet == null) {
                return null;
            }

            var plant = MonsterPlant.Seeded(pet.PetId, request.Rarity, request.PlantedAt);
            Insert(connection, transaction, plant);
            transaction.Commit();
            pet.Plant = plant;

            return pet;
        }
        catch (Exception exception) {
            logger.LogError(exception, "Could not plant seed {ItemId} for user {OwnerId} in room {RoomId}",
                request.ItemId, request.OwnerId, request.RoomId);

            return null;
        }
    }

    public void Save(MonsterPlant plant)
    {
        using var connection = database.Connection();
        var updated = connection.Execute("""
            UPDATE bots_monsterplants
            SET growth_started_at = @GrowthStartedAt, wellbeing_until = @WellbeingUntil,
                can_breed = @CanBreed, breeding_permission = @BreedingPermission
            WHERE pet_id = @PetId LIMIT 1
            """, Row(plant));

        if (updated != 1) {
            throw new InvalidOperationException("The monster plant was not saved.");
        }
    }

    internal static void Insert(IDbConnection connection, IDbTransaction? transaction, MonsterPlant plant) => connection.Execute("""
        INSERT INTO bots_monsterplants (pet_id, rarity, growth_started_at, wellbeing_until, can_breed, breeding_permission)
        VALUES (@PetId, @Rarity, @GrowthStartedAt, @WellbeingUntil, @CanBreed, @BreedingPermission)
        """, Row(plant), transaction);

    internal static Dictionary<int, MonsterPlant> LoadForPets(IDbConnection connection, IEnumerable<int> petIds)
    {
        var plants = new Dictionary<int, MonsterPlant>();

        foreach (var ids in petIds.Chunk(500)) {
            var rows = connection.Query<PlantRow>("""
                SELECT pet_id AS PetId, rarity, growth_started_at AS GrowthStartedAt, wellbeing_until AS WellbeingUntil,
                       can_breed AS CanBreed, breeding_permission AS BreedingPermission
                FROM bots_monsterplants WHERE pet_id IN @ids
                """, new { ids });

            foreach (var row in rows) {
                plants[row.PetId] = new MonsterPlant(row.PetId, row.Rarity, Utc(row.GrowthStartedAt), Utc(row.WellbeingUntil),
                    row.CanBreed, row.BreedingPermission);
            }
        }

        return plants;
    }

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static object Row(MonsterPlant plant) => new
    {
        plant.PetId,
        plant.Rarity,
        GrowthStartedAt = plant.GrowthStartedAt.UtcDateTime,
        WellbeingUntil = plant.WellbeingUntil.UtcDateTime,
        plant.CanBreed,
        plant.BreedingPermission
    };

    private sealed class PlantRow
    {
        public int PetId { get; set; }
        public int Rarity { get; set; }
        public DateTime GrowthStartedAt { get; set; }
        public DateTime WellbeingUntil { get; set; }
        public bool CanBreed { get; set; }
        public bool BreedingPermission { get; set; }
    }
}
