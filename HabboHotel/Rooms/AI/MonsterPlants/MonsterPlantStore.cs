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
    public Pet? Plant(MonsterPlantSeedRequest request) => throw new NotImplementedException();

    public void Save(MonsterPlant plant) => throw new NotImplementedException();

    internal static void Insert(IDbConnection connection, IDbTransaction? transaction, MonsterPlant plant) => throw new NotImplementedException();

    internal static MonsterPlant? Load(IDbConnection connection, int petId) => throw new NotImplementedException();

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
        public int Rarity { get; set; }
        public DateTime GrowthStartedAt { get; set; }
        public DateTime WellbeingUntil { get; set; }
        public bool CanBreed { get; set; }
        public bool BreedingPermission { get; set; }
    }
}
