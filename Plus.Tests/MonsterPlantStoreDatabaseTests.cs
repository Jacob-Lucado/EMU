using System.Data;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
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

            return new(root, schema, connection, new MonsterPlantStore(new ProbeDatabase(cs), NullLogger<MonsterPlantStore>.Instance));
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
