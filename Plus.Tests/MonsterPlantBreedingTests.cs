using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using Plus.Communication.Packets.Incoming.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Outgoing.Pets;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantBreedingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2040-01-01T00:00:00Z");

    private static readonly ItemDefinition Seed = new()
    {
        Id = 100,
        SpriteId = 4582,
        Type = ItemType.Floor,
        ItemName = "mnstr_seed",
        SpecialType = (int)FurniCategory.MonsterplantSeed
    };

    [Fact]
    public async Task TheOwnerBreedsTwoOwnGrownPlantsIntoOneSeed()
    {
        var world = World();

        await world.Request(0, 21, 22);

        var bred = Assert.Single(world.Store.Bred);
        Assert.Equal((21, false, 22, false, "mnstr_seed", 1, 1),
            (bred.First.PetId, bred.First.CanBreed, bred.Second.PetId, bred.Second.CanBreed, bred.Seed.ItemName, bred.Owner.Id, bred.Rarity));
        Assert.Equal([(0, 2)], world.Genetics.Bred);
        Assert.False(world.Own.PetData!.Plant!.CanBreed);
        Assert.False(world.Other.PetData!.Plant!.CanBreed);
        Assert.Equal((0, Now.AddDays(2)), (world.Own.PetData.Plant.Rarity, world.Own.PetData.Plant.WellbeingUntil));
        var seed = Assert.Single(world.Client.GetHabbo().Inventory.Furniture.GetItems);
        Assert.Equal((900u, 1), (seed.Id, MonsterPlantSeedRarity.Of(seed.ExtraData)));
    }

    [Fact]
    public async Task BreedingSendsHabbosPacketsInHabbosOrder()
    {
        var world = World();

        await world.Request(0, 21, 22);

        uint[] watched = [ServerPacketHeader.FurniListNotificationComposer, ServerPacketHeader.FurniListAddComposer,
            ServerPacketHeader.PetStatusUpdateComposer, ServerPacketHeader.PetBreedingResultComposer, ServerPacketHeader.ChatComposer];
        Assert.Equal([ServerPacketHeader.FurniListNotificationComposer, ServerPacketHeader.FurniListAddComposer,
                ServerPacketHeader.PetStatusUpdateComposer, ServerPacketHeader.PetStatusUpdateComposer,
                ServerPacketHeader.PetBreedingResultComposer, ServerPacketHeader.ChatComposer],
            world.Sent.Select(p => p.Header).Where(watched.Contains));
        var statuses = world.Sent.Where(p => p.Header == ServerPacketHeader.PetStatusUpdateComposer).Select(p => p.Payload).ToList();
        Assert.Equal([(world.Own.VirtualId, 21), (world.Other.VirtualId, 22)], statuses.Select(p => (Int(p, 0), Int(p, 4))));
        Assert.All(statuses, p => Assert.Equal(new byte[] { 0, 0, 0, 0 }, p[8..12]));
        var result = world.Sent.Single(p => p.Header == ServerPacketHeader.PetBreedingResultComposer).Payload;
        Assert.Equal((900, 4582), (Int(result, 0), Int(result, 4)));
    }

    [Fact]
    public void TheBreedingResultHasHabbosLayout()
    {
        var packet = new HabbiconTestSupport.RecordingPacket();

        new PetBreedingResultComposer(new(929102647, 4582, "mnstr_seed", 1, "johndoe", 0, false), PetBreedingResultSeed.None).Compose(packet);

        Assert.Equal(new object[] { 929102647, 4582, "mnstr_seed", 1, "johndoe", 0, false, -1, -1, "", -1, "", -1, false }, packet.Writes);
    }

    [Fact]
    public async Task TheRequestingPlantCelebratesAndSpeaks()
    {
        var world = World();

        await world.Request(0, 21, 22);

        Assert.Equal("rev", world.Own.Statusses["gst"]);
        Assert.True(world.Own.UpdateNeeded);
        Assert.False(world.Other.Statusses.ContainsKey("gst"));
        var chat = world.Sent.Single(p => p.Header == ServerPacketHeader.ChatComposer).Payload;
        Assert.Equal(world.Own.VirtualId, Int(chat, 0));
        Assert.Contains("Now I know how Pete Rose felt.", Encoding.UTF8.GetString(chat));
    }

    [Fact]
    public async Task BreedingProgressesTheBreederAchievementAndTheBreedTask()
    {
        var world = World();

        await world.Request(0, 21, 22);

        Assert.Equal([("ACH_MonsterPlantBreeder", 1), ("breed_monster_plant", 1)], world.Progress);
        Assert.Equal(2, world.Sent.Count(p => p.Header == ServerPacketHeader.FurniListAddComposer));
    }

    [Fact]
    public async Task AFailedBreedProgressesNothing()
    {
        var world = World();
        world.Store.Fail = true;

        await world.Request(0, 21, 22);
        world.Store.Fail = false;
        await world.Request(1, 21, 22);

        Assert.Empty(world.Progress);
    }

    [Fact]
    public async Task TheCelebrationLastsOneStatusUpdate()
    {
        var world = World();
        await world.Request(0, 21, 22);

        world.Own.BotAi.OnTimerTick();
        Assert.Equal("rev", world.Own.Statusses["gst"]);

        world.Own.UpdateNeeded = false;
        world.Own.BotAi.OnTimerTick();

        Assert.False(world.Own.Statusses.ContainsKey("gst"));
        Assert.True(world.Own.UpdateNeeded);
    }

    [Fact]
    public async Task APlantDoesNotRepeatItsNewStateOnTheNextTick()
    {
        var world = World();
        await world.Request(0, 21, 22);
        world.Sent.Clear();

        world.Own.BotAi.OnTimerTick();
        world.Other.BotAi.OnTimerTick();

        Assert.DoesNotContain(world.Sent, p => p.Header == ServerPacketHeader.PetStatusUpdateComposer);
    }

    [Theory]
    [InlineData("other owner")]
    [InlineData("other owner allows breeding")]
    [InlineData("not my plant")]
    [InlineData("same plant")]
    [InlineData("growing")]
    [InlineData("bred before")]
    [InlineData("dead")]
    [InlineData("unknown pet")]
    [InlineData("cancel")]
    [InlineData("accept")]
    public async Task AnInvalidRequestBreedsNothing(string scenario)
    {
        var world = World();
        var (state, own, other) = (0, 21, 22);

        switch (scenario) {
            case "other owner":
                world.Other.PetData!.OwnerId = 2;
                break;
            case "other owner allows breeding":
                world.Other.PetData!.OwnerId = 2;
                world.Other.PetData.Plant!.BreedingPermission = true;
                break;
            case "not my plant":
                world.Own.PetData!.OwnerId = 2;
                break;
            case "same plant":
                other = 21;
                break;
            case "growing":
                world.Other.PetData!.Plant = new MonsterPlant(22, 2, Now.AddHours(-1), Now.AddDays(1), true, false);
                break;
            case "bred before":
                world.Other.PetData!.Plant = new MonsterPlant(22, 2, Now.AddDays(-3), Now.AddDays(2), false, false);
                break;
            case "dead":
                world.Other.PetData!.Plant = new MonsterPlant(22, 2, Now.AddDays(-5), Now.AddSeconds(-1), true, false);
                break;
            case "unknown pet":
                other = 99;
                break;
            case "cancel":
                state = 1;
                break;
            case "accept":
                state = 2;
                break;
        }

        await world.Request(state, own, other);

        Assert.Empty(world.Store.Bred);
        Assert.Empty(world.Client.GetHabbo().Inventory.Furniture.GetItems);
        Assert.DoesNotContain(world.Sent, p => p.Header is ServerPacketHeader.PetBreedingResultComposer or ServerPacketHeader.PetStatusUpdateComposer);
    }

    [Fact]
    public async Task AFailedSaveKeepsBothPlantsBreedable()
    {
        var world = World();
        world.Store.Fail = true;

        await world.Request(0, 21, 22);

        Assert.Single(world.Store.Bred);
        Assert.True(world.Own.PetData!.Plant!.CanBreed);
        Assert.True(world.Other.PetData!.Plant!.CanBreed);
        Assert.Empty(world.Client.GetHabbo().Inventory.Furniture.GetItems);
        Assert.Empty(world.Sent);
    }

    [Fact]
    public async Task ARepeatedRequestBreedsOnce()
    {
        var world = World();

        await world.Request(0, 21, 22);
        await world.Request(0, 21, 22);
        await world.Request(0, 22, 21);

        Assert.Single(world.Store.Bred);
    }

    [Theory]
    [InlineData(0, 2, 0, 0)]
    [InlineData(0, 2, 2, 2)]
    [InlineData(4, 1, 0, 1)]
    [InlineData(4, 1, 3, 4)]
    [InlineData(3, 3, 0, 3)]
    public void ABredSeedHasARarityBetweenItsParents(int first, int second, int pick, int rarity)
    {
        var bounds = new List<int>();

        var bred = new MonsterPlantGenetics(max => { bounds.Add(max); return pick; }).BreedRarity(first, second);

        Assert.Equal(rarity, bred);
        Assert.Equal([Math.Abs(first - second) + 1], bounds);
    }

    private static int Int(byte[] payload, int offset) => BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(offset, 4));

    private static Fixture World()
    {
        var model = new RoomModel("breed-test", 0, 0, 0, 0, "00000\r00000\r00000", 0, 0, false);
        var room = new Room(new RoomData { Id = 42, Model = model }, [], TestLogging.Navigation, TestLogging.Logger, TestRoomAchievements.Unused, TestRoomOwners.Unused);
        Set(room, "_interactionClock", TimeProvider.System);
        Set(room, "_roomItemHandling", new RoomItemHandling(room, TestRoomItemStore.Instance, TestRoomItemMetadataStore.Instance, TestGameClientManager.Empty, TestLanguageManager.RoomItems, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));
        Set(room, "_gamemap", new Gamemap(room, model, TestLogging.Navigation, TestRoomSettings.Empty, TestGroupManager.Empty, TestNavigationDatabase.Instance, TestNavigationRewards.Instance));
        Set(room, "_roomUserManager", new RoomUserManager(room, TestRoomUserStore.Instance, new FixedClock(), new TestRewardProgress(), TestChatEmotions.Unused, TestBotAiFactory.Inert, TestGameClientManager.Empty, TestItemRuntime.Travel, TestItemRuntime.Settings));
        Set(room, "_userSnapshots", CatalogSnapshotTestSupport.Proxy<IRoomUserSnapshotService>((_, _) => null));
        room.GetGameMap().GenerateMaps();
        var (client, sent) = HabbiconTestSupport.Client(new Habbo
        {
            Id = 1,
            Username = "johndoe",
            CurrentRoom = room,
            Inventory = new InventoryComponent { Furniture = new FurnitureInventoryComponent([], []) }
        });
        Assert.True(room.GetRoomUserManager().AddAvatarToRoom(client));
        var own = Deploy(room, 21, "Aenueus Squarg", 0, 1, 1);
        var other = Deploy(room, 22, "Fulvus Weggytum", 2, 3, 1);
        sent.Clear();
        var store = new RecordingStore();
        var genetics = new RecordingGenetics();
        var items = CatalogSnapshotTestSupport.Proxy<IItemDataManager>((method, args) =>
            method == nameof(IItemDataManager.GetItemByName) && (string)args[0]! == "mnstr_seed" ? Seed : null);

        var progress = new List<(string Action, int SeedUpdates)>();
        void RecordProgress(string action) => progress.Add((action, sent.Count(p => p.Header == ServerPacketHeader.FurniListAddComposer)));
        var achievements = CatalogSnapshotTestSupport.Proxy<IAchievementManager>((_, args) => { RecordProgress((string)args[1]!); return true; });
        var rewards = CatalogSnapshotTestSupport.Proxy<IRewardTrackManager>((_, args) => { RecordProgress((string)args[1]!); return null; });

        return new(room, client, sent, own, other, store, genetics,
            new MonsterPlantBreedingService(store, genetics, items, achievements, rewards, new FixedClock()), progress);
    }

    private static RoomUser Deploy(Room room, int petId, string name, int rarity, int x, int y)
    {
        var pet = new Pet(petId, 1, room.RoomId, name, MonsterPlant.PetType, "0", "ffffff", 0, 100, 100, 0, Now.AddDays(-3), x, y, 0, 0, 0, 0, -1,
            "2 0 -1 1 1 9 9", "johndoe")
        {
            Plant = new MonsterPlant(petId, rarity, Now.AddDays(-3), Now.AddDays(2), true, false)
        };
        var speeches = new List<RandomSpeech>();
        var user = room.GetRoomUserManager().DeployBot(new(petId, room.RoomId, "pet", "stand", name, "", pet.Look, x, y, 0, 0, 0, 0, 0, 0,
            ref speeches, "", 0, 1, false, 0, false, 0), pet);
        user.BotAi.OnSelfEnterRoom();
        user.UpdateNeeded = false;

        return user;
    }

    private static void Set(object instance, string field, object value) => instance.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);

    private sealed record Fixture(Room Room, GameClient Client, List<(uint Header, byte[] Payload)> Sent, RoomUser Own, RoomUser Other,
        RecordingStore Store, RecordingGenetics Genetics, MonsterPlantBreedingService Service, List<(string Action, int SeedUpdates)> Progress)
    {
        public Task Request(int state, int ownPetId, int otherPetId) =>
            new BreedPetsEvent(Service).Parse(Room, Client, HabbiconTestSupport.Incoming(state, ownPetId, otherPetId));
    }

    private sealed class RecordingGenetics : IMonsterPlantGenetics
    {
        public List<(int, int)> Bred { get; } = [];

        public MonsterPlantSeedling Roll(ItemDefinition seed, int? rarity) => throw new InvalidOperationException("Breeding plants nothing.");

        public int BreedRarity(int first, int second)
        {
            Bred.Add((first, second));

            return 1;
        }
    }

    private sealed class RecordingStore : IMonsterPlantStore
    {
        public List<(MonsterPlant First, MonsterPlant Second, ItemDefinition Seed, Habbo Owner, int Rarity)> Bred { get; } = [];
        public bool Fail;

        public Pet? Plant(MonsterPlantSeedRequest request) => throw new InvalidOperationException("Breeding plants nothing.");

        public void Save(MonsterPlant plant) => throw new InvalidOperationException("Breeding saves both plants together.");

        public Item? Breed(MonsterPlant first, MonsterPlant second, ItemDefinition seed, Habbo owner, int rarity)
        {
            Bred.Add((first, second, seed, owner, rarity));

            return Fail
                ? null
                : new Item
                {
                    Id = 900,
                    OwnerId = (uint)owner.Id,
                    Definition = seed,
                    ExtraData = FurniExtraData.Load(seed, "rarity\t" + rarity, keepLegacy: false)
                };
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
