using System.Collections.Concurrent;
using System.Reflection;
using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantSeedServiceTests
{
    [Fact]
    public void OwnerPlantsTheSeedWhereItStood()
    {
        var world = World();

        Assert.True(world.Service.TryPlant(world.Room, world.Client, 7));

        var request = Assert.Single(world.Store.Requests);
        Assert.Equal((7u, 100u, 1, "owner", 42u, 2, 1, 0.5, world.Clock.Now),
            (request.ItemId, request.BaseItem, request.OwnerId, request.OwnerName, request.RoomId,
                request.X, request.Y, request.Z, request.PlantedAt));
        Assert.Equal(("Sunspike", 3), (request.Name, request.Rarity));
        Assert.Null(world.Room.GetRoomItemHandler().GetItem(7));
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var actor));
        Assert.Equal((2, 1), (actor.X, actor.Y));
        Assert.Equal(MonsterPlant.PetType, Assert.IsType<Pet>(actor.PetData).Type);
        Assert.NotNull(actor.PetData!.Plant);
    }

    [Fact]
    public void ASeedGrowsIntoAPlantOfItsRarity()
    {
        var world = World();
        world.Item.ExtraData = FurniExtraData.Load(world.Item.Definition, "rarity\t2", keepLegacy: true);

        world.Service.TryPlant(world.Room, world.Client, 7);

        Assert.Equal([2], world.Genetics.Rarities);
    }

    [Fact]
    public void ASeedWithoutARarityRollsAnyLook()
    {
        var world = World();

        world.Service.TryPlant(world.Room, world.Client, 7);

        Assert.Equal([null], world.Genetics.Rarities);
    }

    [Fact]
    public void PlantingASeedProgressesThePlantMonsterPlantTask()
    {
        var world = World();

        world.Service.TryPlant(world.Room, world.Client, 7);
        world.Service.TryPlant(world.Room, world.Client, 7);

        Assert.Equal(["plant_monster_plant"], world.Rewards);
    }

    [Fact]
    public void OnlyTheSeedOwnerCanPlantIt()
    {
        var world = World();
        world.Item.OwnerId = 99;

        Assert.True(world.Service.TryPlant(world.Room, world.Client, 7));

        Assert.Empty(world.Store.Requests);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
    }

    [Fact]
    public void OtherFurniIsNotASeed()
    {
        var world = World();
        world.Item.Definition.SpecialType = 1;

        Assert.False(world.Service.TryPlant(world.Room, world.Client, 7));
        Assert.Empty(world.Store.Requests);
    }

    [Fact]
    public void FailedSaveKeepsTheSeed()
    {
        var world = World();
        world.Store.Fail = true;

        Assert.True(world.Service.TryPlant(world.Room, world.Client, 7));

        Assert.Single(world.Store.Requests);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
        Assert.False(world.Room.GetRoomUserManager().TryGetPet(12, out _));
    }

    [Fact]
    public void UsingTheSeedTwicePlantsOnce()
    {
        var world = World();

        world.Service.TryPlant(world.Room, world.Client, 7);
        world.Service.TryPlant(world.Room, world.Client, 7);

        Assert.Single(world.Store.Requests);
    }

    [Fact]
    public async Task UseFurnitureOnASeedPlantsItInsteadOfTogglingIt()
    {
        var world = World();
        var furniture = new RecordingFurniture();

        await new UseFurnitureEvent(furniture, world.Service).Parse(world.Room, world.Client, HabbiconTestSupport.Incoming(7, 0));

        Assert.Single(world.Store.Requests);
        Assert.Equal(0, furniture.Uses);
    }

    [Fact]
    public void APlantedPlantStaysOnItsTileAndStaysQuiet()
    {
        var world = World(new TestBotAiFactory());
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var plant));
        var sent = world.Sent.Count;

        plant.BotAi.OnSelfEnterRoom();

        for (var tick = 0; tick < 500; tick++) {
            plant.BotAi.OnTimerTick();
        }

        plant.BotAi.OnUserEnterRoom(world.Room.GetRoomUserManager().GetRoomUserByHabbo(1)!);

        Assert.Equal((2, 1, false), (plant.X, plant.Y, plant.IsWalking));
        Assert.Equal(sent, world.Sent.Count);
    }

    [Fact]
    public void APlantShowsItsGrowthPostureAndAnnouncesNewLevelsAndDeath()
    {
        var clock = new MovingClock();
        var world = World(time: clock);
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var plant));
        plant.PetData!.Plant = MonsterPlant.Seeded(12, 0, clock.Now);
        Assert.True(world.Room.GetRoomUserManager().AddAvatarToRoom(world.Client));
        plant.BotAi.OnSelfEnterRoom();
        Assert.Equal(["grw1"], plant.Statusses.Keys);
        world.Sent.Clear();

        clock.Now = clock.Now.AddHours(8);
        plant.BotAi.OnTimerTick();

        Assert.Equal(["grw2"], plant.Statusses.Keys);
        Assert.True(plant.UpdateNeeded);
        Assert.Equal([ServerPacketHeader.PetLevelUpdateComposer], world.Sent.Select(p => p.Header));
        world.Sent.Clear();

        plant.BotAi.OnTimerTick();
        Assert.Empty(world.Sent);

        clock.Now = clock.Now.AddHours(36);
        plant.BotAi.OnTimerTick();

        Assert.Equal(["rip"], plant.Statusses.Keys);
        Assert.Equal([ServerPacketHeader.PetLevelUpdateComposer, ServerPacketHeader.PetStatusUpdateComposer], world.Sent.Select(p => p.Header));
    }

    [Fact]
    public void TreatingAPlantFillsItsWellbeingWithoutUsingRespectPoints()
    {
        var clock = new MovingClock();
        var world = World(time: clock);
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var plant));
        plant.PetData!.Plant = new MonsterPlant(12, 4, clock.Now.AddDays(-10), clock.Now.AddSeconds(249350), false, true);
        plant.PetData.Respect = 47;
        Assert.True(world.Room.GetRoomUserManager().AddAvatarToRoom(world.Client));
        world.Client.GetHabbo().HabboStats = new HabboStats(0, 0, 0, 0, 0, 0, 0, 0, 0, "");
        var achievements = new List<string>();
        var saved = new List<MonsterPlant>();
        world.Sent.Clear();

        Respects(achievements, saved, clock).Respect(world.Room, world.Client, 12);

        Assert.Equal(TimeSpan.FromSeconds(259200), plant.PetData.Plant.RemainingWellbeing(clock.Now));
        Assert.Equal(48, plant.PetData.Respect);
        Assert.Equal(0, world.Client.GetHabbo().HabboStats!.DailyPetRespectPoints);
        Assert.Equal(["ACH_MonsterPlantTreater"], achievements);
        Assert.Same(plant.PetData.Plant, Assert.Single(saved));
        Assert.Contains(world.Sent, p => p.Header == ServerPacketHeader.RespectPetNotificationComposer);
    }

    [Fact]
    public void AFaultWhileSavingATreatKeepsThePreviousPlantState()
    {
        var clock = new MovingClock();
        var world = World(time: clock);
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var user));
        var pet = user.PetData!;
        var expiry = clock.Now.AddHours(1);
        pet.Plant = new MonsterPlant(12, 4, clock.Now.AddDays(-10), expiry, false, true);
        var previousRespect = pet.Respect;
        var store = CatalogSnapshotTestSupport.Proxy<IMonsterPlantStore>((_, _) => throw new InvalidOperationException("save failed"));
        var service = new PetRespectService(null!, null!, null!, store, clock);
        world.Sent.Clear();

        Assert.Throws<InvalidOperationException>(() => service.Respect(world.Room, world.Client, pet.PetId));
        Assert.Equal(expiry, pet.Plant.WellbeingUntil);
        Assert.Equal(previousRespect, pet.Respect);
        Assert.Empty(world.Sent);
    }

    [Fact]
    public void ADeadPlantCannotBeTreated()
    {
        var clock = new MovingClock();
        var world = World(time: clock);
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var plant));
        plant.PetData!.Plant = new MonsterPlant(12, 4, clock.Now.AddDays(-10), clock.Now.AddDays(-1), false, true);
        Assert.True(world.Room.GetRoomUserManager().AddAvatarToRoom(world.Client));
        world.Client.GetHabbo().HabboStats = new HabboStats(0, 0, 0, 0, 0, 3, 0, 0, 0, "");
        var achievements = new List<string>();
        var saved = new List<MonsterPlant>();
        world.Sent.Clear();

        Respects(achievements, saved, clock).Respect(world.Room, world.Client, 12);

        Assert.Empty(achievements);
        Assert.Empty(saved);
        Assert.Equal(3, world.Client.GetHabbo().HabboStats!.DailyPetRespectPoints);
        Assert.Empty(world.Sent);
    }

    [Fact]
    public void ATreatedPlantIsAnnouncedInHabbosRespectLayout()
    {
        var packet = new HabbiconTestSupport.RecordingPacket();

        new Plus.Communication.Packets.Outgoing.Pets.PetTreatedComposer(48, 64450688, 8217639, "Fulvus Wystique", 16, 0, "FFFFFF",
            "5 1 10 9 3 9 8 4 2 1 2 10 6 0 -1 0", 7, 4).Compose(packet);

        Assert.Equal(new object[] { 48, 64450688, 8217639, "Fulvus Wystique", 16, 0, "ffffff", 0,
            5, 1, 10, 9, 3, 9, 8, 4, 2, 1, 2, 10, 6, 0, -1, 0, 7, 4 }, packet.Writes);
    }

    [Fact]
    public void TreatingAPlantProgressesTheTreatMonsterPlantTask()
    {
        var clock = new MovingClock();
        var world = World(time: clock);
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var plant));
        plant.PetData!.Plant = new MonsterPlant(12, 4, clock.Now.AddDays(-10), clock.Now.AddDays(1), false, true);
        Assert.True(world.Room.GetRoomUserManager().AddAvatarToRoom(world.Client));
        var rewards = new List<string>();
        var respects = new PetRespectService(
            CatalogSnapshotTestSupport.Proxy<IAchievementManager>((_, _) => true),
            CatalogSnapshotTestSupport.Proxy<IQuestManager>((_, _) => null),
            CatalogSnapshotTestSupport.Proxy<IRewardTrackManager>((_, args) => { rewards.Add((string)args[1]!); return null; }),
            new SavingStore([]), clock);

        respects.Respect(world.Room, world.Client, 12);

        Assert.Equal(["treat_monster_plant"], rewards);
    }

    private static PetRespectService Respects(List<string> achievements, List<MonsterPlant> saved, TimeProvider clock) => new(
        CatalogSnapshotTestSupport.Proxy<IAchievementManager>((method, args) =>
        {
            achievements.Add((string)args[1]!);

            return true;
        }),
        CatalogSnapshotTestSupport.Proxy<IQuestManager>((_, _) => null),
        CatalogSnapshotTestSupport.Proxy<IRewardTrackManager>((_, _) => null),
        new SavingStore(saved), clock);

    [Fact]
    public void ADeadGrownPlantShowsInTheUsersPacketLikeHabbo()
    {
        var world = World();
        world.Service.TryPlant(world.Room, world.Client, 7);
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var actor));
        actor.PetData!.Plant = new MonsterPlant(12, 2, world.Clock.Now.AddDays(-365), world.Clock.Now.AddDays(-1), false, false);
        var clients = CatalogSnapshotTestSupport.Proxy<IGameClientManager>((_, _) => null);
        var snapshots = new RoomUserSnapshotService(null!, clients, null!, null!, TestBadgeRarity.Empty, world.Clock);
        var packet = new HabbiconTestSupport.RecordingPacket();

        new UsersComposer(Assert.IsType<RoomUserSnapshot>(snapshots.Capture(actor))).Compose(packet);

        Assert.Equal(new object[] { 16, 1, "owner", 2, false, false, false, false, true, false, 7, "rip" }, packet.Writes.TakeLast(12));
    }

    private static Fixture World(IBotAiFactory? bots = null, TimeProvider? time = null)
    {
        var model = new RoomModel("plant-test", 0, 0, 0, 0, "000\r000\r000", 0, 0, false);
        var room = new Room(new RoomData { Id = 42, Model = model }, [], TestLogging.Navigation, TestLogging.Logger, TestRoomAchievements.Unused, TestRoomOwners.Unused);
        Set(room, "_interactionClock", TimeProvider.System);
        Set(room, "_roomItemHandling", new RoomItemHandling(room, TestRoomItemStore.Instance, TestRoomItemMetadataStore.Instance, TestGameClientManager.Empty, TestLanguageManager.RoomItems, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));
        Set(room, "_gamemap", new Gamemap(room, model, TestLogging.Navigation, TestRoomSettings.Empty, TestGroupManager.Empty, TestNavigationDatabase.Instance, TestNavigationRewards.Instance));
        Set(room, "_roomUserManager", new RoomUserManager(room, TestRoomUserStore.Instance, time ?? TimeProvider.System, new TestRewardProgress(), TestChatEmotions.Unused, bots ?? TestBotAiFactory.Inert, TestGameClientManager.Empty, TestItemRuntime.Travel, TestItemRuntime.Settings));
        Set(room, "_userSnapshots", CatalogSnapshotTestSupport.Proxy<IRoomUserSnapshotService>((_, _) => null));
        var item = new Item
        {
            Id = 7,
            BaseItem = 100,
            OwnerId = 1,
            RoomId = 42,
            Definition = new() { Id = 100, Type = ItemType.Floor, ItemName = "mnstr_seed", SpecialType = (int)FurniCategory.MonsterplantSeed },
            ExtraData = new LegacyDataFormat { Data = "" }
        };
        item.SetState(2, 1, 0.5);
        item.Attach(room, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards);
        var floor = (ConcurrentDictionary<uint, Item>)typeof(RoomItemHandling).GetField("_floorItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(room.GetRoomItemHandler())!;
        floor.AddOrUpdate(7, item, (_, _) => item);
        room.GetGameMap().GenerateMaps();
        var (client, sent) = HabbiconTestSupport.Client(new Habbo
        {
            Id = 1,
            Username = "owner",
            CurrentRoom = room,
            Inventory = new InventoryComponent { Furniture = new FurnitureInventoryComponent([], []) }
        });
        var store = new RecordingStore();
        var clock = new FixedClock();

        var rewards = new List<string>();
        var genetics = new FixedGenetics();

        return new(room, item, client, sent, store, clock, new(store, genetics, clock,
            CatalogSnapshotTestSupport.Proxy<IRewardTrackManager>((_, args) => { rewards.Add((string)args[1]!); return null; })), rewards, genetics);
    }

    private static void Set(object instance, string field, object value) => instance.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);

    private sealed record Fixture(Room Room, Item Item, GameClient Client, List<(uint Header, byte[] Payload)> Sent, RecordingStore Store, FixedClock Clock, MonsterPlantSeedService Service, List<string> Rewards, FixedGenetics Genetics);

    private sealed class FixedGenetics : IMonsterPlantGenetics
    {
        public List<int?> Rarities { get; } = [];

        public MonsterPlantSeedling Roll(ItemDefinition seed, int? rarity)
        {
            Rarities.Add(rarity);

            return new("Sunspike", new(0, "FFFFFF", "1 0 -1 0"), 3);
        }

        public int BreedRarity(int first, int second) => 0;
    }

    private sealed class RecordingStore : IMonsterPlantStore
    {
        public List<MonsterPlantSeedRequest> Requests { get; } = [];
        public bool Fail;

        public Pet? Plant(MonsterPlantSeedRequest request)
        {
            Requests.Add(request);

            return Fail
                ? null
                : new Pet(12, request.OwnerId, request.RoomId, request.Name, MonsterPlant.PetType, request.Look.Race.ToString(), request.Look.Color,
                    0, 100, 100, 0, request.PlantedAt, request.X, request.Y, request.Z, 0, 0, 0, -1, request.Look.Parts, request.OwnerName)
                {
                    Plant = new MonsterPlant(12, request.Rarity, request.PlantedAt, request.PlantedAt, true, false)
                };
        }

        public void Save(MonsterPlant plant) { }
        public Item? Breed(MonsterPlant first, MonsterPlant second, ItemDefinition seed, Habbo owner, int rarity) => null;
    }

    private sealed class SavingStore(List<MonsterPlant> saved) : IMonsterPlantStore
    {
        public Pet? Plant(MonsterPlantSeedRequest request) => null;
        public void Save(MonsterPlant plant) => saved.Add(plant);
        public Item? Breed(MonsterPlant first, MonsterPlant second, ItemDefinition seed, Habbo owner, int rarity) => null;
    }

    private sealed class RecordingFurniture : IFurnitureUseService
    {
        public int Uses;
        public void Use(Room room, GameClient session, FurnitureUseRequest request) => Uses++;
        public void Click(Room room, GameClient session, FurnitureClickRequest request) { }
        public void TurnOffDice(Room room, GameClient session, uint itemId) { }
        public void RollDice(Room room, GameClient session, FurnitureUseRequest request) { }
        public void UseOneWayGate(Room room, GameClient session, uint itemId) { }
        public void UseWall(Room room, GameClient session, FurnitureUseRequest request) { }
        public void UseHabboWheel(Room room, GameClient session, uint itemId) { }
    }

    private sealed class MovingClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2040-01-01T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Now { get; } = DateTimeOffset.Parse("2040-01-01T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
