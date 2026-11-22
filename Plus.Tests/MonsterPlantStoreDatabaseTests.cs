using System.Data;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantStoreDatabaseTests
{
    private static readonly DateTimeOffset PlantedAt = DateTimeOffset.Parse("2040-01-01T00:00:00.123456Z");

    [PetPlacementDatabaseFact]
    public void PlantingReplacesTheSeedWithAPlantPetAndItsPlantRow()
    {
        using var scratch = Scratch.Create();
        var request = Request();

        var pet = Assert.IsType<Pet>(scratch.Store.Plant(request));

        Assert.Empty(scratch.Connection.Query<int>("SELECT id FROM items"));
        Assert.Equal((16, "4", "AABBCC", "1 2 3 4"), scratch.Connection.QuerySingle<(int, string, string, string)>(
            "SELECT type, race, color, gnome_clothing FROM bots_petdata WHERE id = @Id", new { Id = pet.PetId }));
        Assert.Equal(("Sunspike", 42u, 3, 1), scratch.Connection.QuerySingle<(string, uint, int, int)>(
            "SELECT name, room_id, x, y FROM bots WHERE id = @Id", new { Id = pet.PetId }));
        var plant = Assert.IsType<MonsterPlant>(pet.Plant);
        Assert.Equal((pet.PetId, 6, PlantedAt), (plant.PetId, plant.Rarity, plant.GrowthStartedAt));
        Assert.Equal(plant.WellbeingUntil, Assert.IsType<MonsterPlant>(MonsterPlantStore.LoadForPets(scratch.Connection, [pet.PetId])[pet.PetId]).WellbeingUntil);
        Assert.Equal(PlantedAt + MonsterPlant.SeedlingMaxWellbeing, plant.WellbeingUntil);
    }

    [PetPlacementDatabaseFact]
    public void ASeedThatIsNotTheOwnersInThatRoomPlantsNothing()
    {
        using var scratch = Scratch.Create();

        foreach (var invalid in new[] { Request() with { OwnerId = 99 }, Request() with { RoomId = 99 }, Request() with { BaseItem = 101 } }) {
            Assert.Null(scratch.Store.Plant(invalid));
        }

        Assert.Equal(0, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
        Assert.Equal(0, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots_monsterplants"));
        Assert.Equal(1, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items"));
    }

    [PetPlacementDatabaseFact]
    public void AFailedPlantRowKeepsTheSeedAndAddsNoPet()
    {
        using var scratch = Scratch.Create();
        scratch.Connection.Execute("CREATE TRIGGER fail_plant BEFORE INSERT ON bots_monsterplants FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='forced plant failure'");

        Assert.Null(scratch.Store.Plant(Request()));

        Assert.Equal(0, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
        Assert.Equal(1, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items"));
    }

    [PetPlacementDatabaseFact]
    public void SavedPlantStateLoadsBack()
    {
        using var scratch = Scratch.Create();
        var pet = Assert.IsType<Pet>(scratch.Store.Plant(Request()));
        var later = new MonsterPlant(pet.PetId, 6, PlantedAt.AddHours(-5), PlantedAt.AddDays(2), false, true);

        scratch.Store.Save(later);

        var loaded = Assert.IsType<MonsterPlant>(MonsterPlantStore.LoadForPets(scratch.Connection, [pet.PetId])[pet.PetId]);
        Assert.Equal((6, PlantedAt.AddHours(-5), PlantedAt.AddDays(2), false, true),
            (loaded.Rarity, loaded.GrowthStartedAt, loaded.WellbeingUntil, loaded.CanBreed, loaded.BreedingPermission));
    }

    [PetPlacementDatabaseFact]
    public void LoadingPlantsReturnsOnlyTheRequestedPets()
    {
        using var scratch = Scratch.Create();
        var first = MonsterPlant.Seeded(16, 2, PlantedAt);
        var second = MonsterPlant.Seeded(17, 3, PlantedAt);
        MonsterPlantStore.Insert(scratch.Connection, null, first);
        MonsterPlantStore.Insert(scratch.Connection, null, second);

        var plants = MonsterPlantStore.LoadForPets(scratch.Connection, [first.PetId, 18]);

        Assert.Equal(first.Rarity, Assert.Single(plants).Value.Rarity);
        Assert.True(plants.ContainsKey(first.PetId));
    }

    [PetPlacementDatabaseFact]
    public void BreedingSavesBothPlantsAndGivesTheOwnerTheSeedTogether()
    {
        using var scratch = Scratch.Create();
        var (first, second) = BreedablePair(scratch);
        var grown = PlantedAt.AddDays(3);

        var seed = Assert.IsType<Item>(scratch.Store.Breed(first.AfterBreeding(grown)!, second.AfterBreeding(grown)!, SeedDefinition, Owner, 2));

        Assert.Equal((1, 4582u, "rarity\t2"), scratch.Connection.QuerySingle<(int, uint, string)>(
            "SELECT user_id, base_item, extra_data FROM items WHERE id = @Id", new { seed.Id }));
        Assert.Equal(2, MonsterPlantSeedRarity.Of(seed.ExtraData));
        Assert.Equal([false, false], scratch.Connection.Query<bool>("SELECT can_breed FROM bots_monsterplants ORDER BY pet_id"));
    }

    [PetPlacementDatabaseFact]
    public void APlantThatBredAlreadyBreedsNothing()
    {
        using var scratch = Scratch.Create();
        var (first, second) = BreedablePair(scratch);
        var grown = PlantedAt.AddDays(3);
        scratch.Connection.Execute("UPDATE bots_monsterplants SET can_breed = 0 WHERE pet_id = @PetId", new { second.PetId });
        var items = scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items");

        Assert.Null(scratch.Store.Breed(first.AfterBreeding(grown)!, second.AfterBreeding(grown)!, SeedDefinition, Owner, 2));

        Assert.Equal(items, scratch.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items"));
        Assert.Equal([true, false], scratch.Connection.Query<bool>("SELECT can_breed FROM bots_monsterplants ORDER BY pet_id"));
    }

    private static readonly ItemDefinition SeedDefinition = new()
    {
        Id = 4582,
        SpriteId = 4582,
        Type = ItemType.Floor,
        ItemName = "mnstr_seed",
        SpecialType = (int)FurniCategory.MonsterplantSeed
    };

    private static readonly Plus.HabboHotel.Users.Habbo Owner = new() { Id = 1, Username = "owner" };

    private static (MonsterPlant First, MonsterPlant Second) BreedablePair(Scratch scratch)
    {
        scratch.Connection.Execute("""
            ALTER TABLE items MODIFY id INT UNSIGNED NOT NULL AUTO_INCREMENT, ADD x INT NOT NULL DEFAULT 0, ADD y INT NOT NULL DEFAULT 0,
                ADD z DOUBLE NOT NULL DEFAULT 0, ADD wall_pos VARCHAR(100) NOT NULL DEFAULT '', ADD rot INT NOT NULL DEFAULT 0,
                ADD extra_data TEXT NOT NULL, ADD limited_number INT NOT NULL DEFAULT 0, ADD limited_stack INT NOT NULL DEFAULT 0
            """);
        var first = new MonsterPlant(16, 0, PlantedAt, PlantedAt.AddDays(5), true, false);
        var second = new MonsterPlant(17, 2, PlantedAt, PlantedAt.AddDays(5), true, false);
        MonsterPlantStore.Insert(scratch.Connection, null, first);
        MonsterPlantStore.Insert(scratch.Connection, null, second);

        return (first, second);
    }

    private static MonsterPlantSeedRequest Request() => new(7, 100, 1, "owner", 42, 3, 1, 0.5, "Sunspike",
        new(4, "AABBCC", "1 2 3 4"), 6, PlantedAt);

    private sealed class Scratch : IDisposable
    {
        private readonly MySqlConnection _root;
        private readonly string _schema;

        private Scratch(MySqlConnection root, string schema, MySqlConnection connection, MonsterPlantStore store)
        {
            _root = root;
            _schema = schema;
            Connection = connection;
            Store = store;
        }

        public MySqlConnection Connection { get; }
        public MonsterPlantStore Store { get; }

        public static Scratch Create()
        {
            var root = new MySqlConnection(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE"));
            root.Open();
            var schema = "plant_" + Guid.NewGuid().ToString("N");
            root.Execute($"CREATE DATABASE `{schema}`");
            var cs = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE"))
            {
                Database = schema,
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true
            }.ConnectionString;
            var connection = new MySqlConnection(cs);
            connection.Open();
            connection.Execute("""
                CREATE TABLE bots (id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY, user_id INT NOT NULL, room_id INT UNSIGNED NOT NULL,
                    name VARCHAR(50) NOT NULL, motto VARCHAR(50) NOT NULL, look VARCHAR(100) NOT NULL,
                    x INT NOT NULL, y INT NOT NULL, z DOUBLE NOT NULL, ai_type VARCHAR(20) NOT NULL) ENGINE=InnoDB;
                CREATE TABLE bots_petdata (id INT UNSIGNED PRIMARY KEY, type INT NOT NULL, race VARCHAR(20) NOT NULL, color VARCHAR(20) NOT NULL,
                    experience INT NOT NULL, energy INT NOT NULL, nutrition INT NOT NULL, respect INT NOT NULL,
                    createstamp DATETIME(6) NULL, have_saddle INT NOT NULL, anyone_ride INT NOT NULL,
                    hairdye INT NOT NULL, pethair INT NOT NULL, gnome_clothing VARCHAR(100) NOT NULL) ENGINE=InnoDB;
                CREATE TABLE items (id INT UNSIGNED PRIMARY KEY, user_id INT NOT NULL, room_id INT UNSIGNED NOT NULL,
                    base_item INT UNSIGNED NOT NULL) ENGINE=InnoDB;
                INSERT INTO items VALUES (7, 1, 42, 100);
                """);
            connection.Execute(MigrationSql());

            return new(root, schema, connection, new MonsterPlantStore(new ProbeDatabase(cs), new ItemFactory(new ProbeDatabase(cs)), NullLogger<MonsterPlantStore>.Instance));
        }

        private static string MigrationSql()
        {
            using var stream = typeof(MonsterPlantStore).Assembly.GetManifestResourceStream("Plus.Migrations.146_MonsterPlants.sql")!;

            return new StreamReader(stream).ReadToEnd();
        }

        public void Dispose()
        {
            Connection.Dispose();
            _root.Execute($"DROP DATABASE `{_schema}`");
            _root.Dispose();
        }
    }

    private sealed class ProbeDatabase(string cs) : IDatabase
    {
        public bool IsConnected() => true;
        public IDbConnection Connection() => new MySqlConnection(cs);
    }
}
