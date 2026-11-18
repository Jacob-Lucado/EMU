using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Incoming.Catalog;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Rooms.Furni;
using Plus.Database;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class GnomePackageServiceTests
{
    [Fact]
    public async Task HandlerConsumesIdAndNameAndDelegates()
    {
        var service = new RecordingService();
        var packet = HabbiconTestSupport.Incoming(7, "Pixel");
        await new CheckGnomeNameEvent(service).Parse(null!, null!, packet);
        Assert.Equal((7u, "Pixel"), service.Request);
        Assert.False(packet.HasDataRemaining());
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("temporary")]
    [InlineData("type")]
    [InlineData("room")]
    [InlineData("current")]
    public void InvalidPackageContextDoesNotReachStore(string denial)
    {
        var world = World(denial == "temporary");

        switch (denial) {
            case "owner":
                world.Item.OwnerId = 99;
                break;
            case "temporary":
                break;
            case "type":
                world.Item.Definition.InteractionType = InteractionType.None;
                break;
            case "room":
                world.Item.RoomId = 99;
                break;
            case "current":
                world.Client.GetHabbo().CurrentRoom = null;
                break;
        }

        world.Service.Open(world.Room, world.Client, 7, "Pixel");
        Assert.Empty(world.Store.Requests);
        Assert.Empty(world.Packets);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
    }

    [Theory]
    [InlineData("", 2, "")]
    [InlineData("bad name!", 3, " !")]
    [InlineData("a#b#", 3, "#")]
    [InlineData("abcdefghijklmnopq", 1, "16")]
    public void InvalidNameRepliesWithHabbosReasonWithoutConsumingPackage(string name, int status, string info)
    {
        var world = World();
        world.Service.Open(world.Room, world.Client, 7, name);
        Assert.Empty(world.Store.Requests);
        Assert.Equal(Bytes(7, status, info), Assert.Single(world.Packets).Payload);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
    }

    [Fact]
    public void FailedTransactionLeavesPackageAndRoomPetsUnchanged()
    {
        var world = World();
        world.Store.Fail = true;
        world.Service.Open(world.Room, world.Client, 7, "Pixel");
        Assert.Single(world.Store.Requests);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
        Assert.False(world.Room.GetRoomUserManager().TryGetPet(12, out _));
        Assert.Single(world.Packets); // Existing failure notification only.
    }

    [Fact]
    public void SuccessPersistsBeforePublicationAndDoubleOpenCreatesOnePet()
    {
        var world = World();
        world.Store.Before = request =>
        {
            Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
            Assert.False(world.Room.GetRoomUserManager().TryGetPet(12, out _));
            Assert.Empty(world.Packets);
            Assert.Equal((7u, 100u, 1, "owner", 42u, 1, 1, 0d, "Pixel"),
                (request.ItemId, request.BaseItem, request.OwnerId, request.OwnerName, request.RoomId,
                    request.X, request.Y, request.Z, request.Name));
            Assert.Equal(world.Clock.Now, request.CreatedAt);
        };
        world.Service.Open(world.Room, world.Client, 7, "Pixel");
        world.Service.Open(world.Room, world.Client, 7, "Again");
        Assert.Single(world.Store.Requests);
        Assert.Null(world.Room.GetRoomItemHandler().GetItem(7));
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var actor));
        Assert.Equal(("owner", "Pixel", 26, 42u),
            (Assert.IsType<Pet>(actor.PetData).OwnerName, Assert.IsType<Pet>(actor.PetData).Name, Assert.IsType<Pet>(actor.PetData).Type, Assert.IsType<Pet>(actor.PetData).RoomId));
        Assert.Equal(1, world.Clock.Reads);
        Assert.NotEmpty(world.Packets);
    }

    [Fact]
    public void OpenPetPackageRequestCarriesThePackageFigure()
    {
        var (client, packets) = HabbiconTestSupport.Client(new Habbo { Id = 1 });

        client.Send(new GnomeBoxComposer(7, new PetPackageFigure(1, 0, "ABABAB")));
        client.Send(new GnomeBoxComposer(8));

        Assert.Equal(Bytes(7, 1, 0, "ABABAB", 0, 0), packets[0].Payload);
        Assert.Equal(Bytes(8), packets[1].Payload);
    }

    [Fact]
    public void OpenPetPackageResultNamesThePackage()
    {
        var (client, packets) = HabbiconTestSupport.Client(new Habbo { Id = 1 });

        client.Send(new CheckGnomeNameComposer(7, "Smokey", PetPackageNameError.None));
        client.Send(new CheckGnomeNameComposer(7, "#", PetPackageNameError.InvalidCharacters));

        Assert.Equal(Bytes(7, 0, ""), packets[0].Payload);
        Assert.Equal(Bytes(7, 3, "#"), packets[1].Payload);
    }

    [Theory]
    [InlineData("1 0 ABABAB", 1, 0, "ABABAB")]
    [InlineData("2 0 88E70D", 2, 0, "88E70D")]
    [InlineData("", 37, 0, "FFFFFF")]
    [InlineData("cat 0 ABABAB", 37, 0, "FFFFFF")]
    [InlineData("1 0 ABABA", 37, 0, "FFFFFF")]
    public void PackageFigureIsReadFromItsState(string state, int type, int palette, string colour) =>
        Assert.Equal(new PetPackageFigure(type, palette, colour), PetPackageFigure.Of(new PetPackage(37, "newbie_nest"), state));

    [Fact]
    public void OpeningAPetPackageLeavesItsFurniWithTheNamedPetOnIt()
    {
        var locale = CatalogSnapshotTestSupport.Proxy<Plus.HabboHotel.Rooms.Chat.Pets.Locale.IPetLocale>((_, _) => new[] { "Welcome home" });
        var ai = new TestBotAiFactory((_, id) => new Plus.HabboHotel.Rooms.AI.Types.PetBot(id, locale, null!));
        var world = World(package: true, botAi: ai);
        TestRoomUserSnapshots.Install(world.Room);
        var viewer = new RoomUser(1, 42, 100, world.Room, world.Client, TestChatEmotions.Unused, new TestRewardProgress());
        var users = (ConcurrentDictionary<int, RoomUser>)typeof(RoomUserManager).GetField("_users", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(world.Room.GetRoomUserManager())!;
        users.TryAdd(viewer.VirtualId, viewer);

        world.Service.Open(world.Room, world.Client, 7, "Smokey");

        var request = Assert.Single(world.Store.PetRequests);
        Assert.Equal((7u, 100u, 1, "owner", 42u, 1, 1, "Smokey", new PetPackageFigure(1, 0, "ABABAB"), 101u, 4, 0.1),
            (request.ItemId, request.BaseItem, request.OwnerId, request.OwnerName, request.RoomId, request.X, request.Y, request.Name,
                request.Figure, request.FurniBaseItem, request.FurniRotation, request.PetZ));
        Assert.Empty(world.Store.Requests);
        Assert.Null(world.Room.GetRoomItemHandler().GetItem(7));
        var nest = Assert.IsType<Item>(world.Room.GetRoomItemHandler().GetItem(13));
        Assert.Equal((101u, 1, 1, 4), (nest.Definition.Id, nest.GetX, nest.GetY, nest.Rotation));
        Assert.True(world.Room.GetRoomUserManager().TryGetPet(12, out var actor));
        var pet = Assert.IsType<Pet>(actor.PetData);
        Assert.Equal(("Smokey", 1, "0", "ABABAB"), (pet.Name, pet.Type, pet.Race, pet.Color));
        var result = world.Packets.FindLastIndex(packet => packet.Header == ServerPacketHeader.CheckGnomeNameComposer);
        Assert.Equal(Bytes(7, 0, ""), world.Packets[result].Payload);
        var order = new[] { ServerPacketHeader.ObjectAddComposer, ServerPacketHeader.ObjectRemoveComposer, ServerPacketHeader.UsersComposer,
            ServerPacketHeader.CheckGnomeNameComposer, ServerPacketHeader.ChatComposer };
        Assert.Equal(order, world.Packets.Select(packet => packet.Header).Where(order.Contains).ToArray());
        Assert.DoesNotContain(world.Packets, packet => packet.Header == ServerPacketHeader.FurniListNotificationComposer);
    }

    [Fact]
    public void NestKeepsTheStoredHeightOfARaisedPackage()
    {
        var world = World(package: true);
        world.Item.SetState(1, 1, 1.5);

        world.Service.Open(world.Room, world.Client, 7, "Smokey");

        var request = Assert.Single(world.Store.PetRequests);
        var nest = Assert.IsType<Item>(world.Room.GetRoomItemHandler().GetItem(13));
        Assert.Equal(1.5, request.Z);
        Assert.Equal(request.Z, nest.GetZ);
        Assert.False(world.Client.GetHabbo().Inventory!.Furniture.HasItem(nest.Id));
    }

    [Fact]
    public void PackageStaysClosedWhenItsNestWouldExtendOutsideTheRoom()
    {
        var world = World(package: true, nestWidth: 4);

        world.Service.Open(world.Room, world.Client, 7, "Smokey");

        Assert.Empty(world.Store.PetRequests);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
        Assert.False(world.Client.GetHabbo().Inventory!.Furniture.HasItem(13));
    }

    [Fact]
    public void InvalidPetNameKeepsThePackageAndRepliesWithItsId()
    {
        var world = World(package: true);

        world.Service.Open(world.Room, world.Client, 7, "bad name!");

        Assert.Empty(world.Store.PetRequests);
        Assert.Equal(Bytes(7, 3, " !"), Assert.Single(world.Packets).Payload);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
    }

    [Fact]
    public void PetPackageWithoutItsFurniStaysClosed()
    {
        var world = World(package: true, withNest: false);

        world.Service.Open(world.Room, world.Client, 7, "Smokey");

        Assert.Empty(world.Store.PetRequests);
        Assert.Empty(world.Packets);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
    }

    [Fact]
    public void FailedPetPackageWriteKeepsThePackage()
    {
        var world = World(package: true);
        world.Store.Fail = true;

        world.Service.Open(world.Room, world.Client, 7, "Smokey");

        Assert.Single(world.Store.PetRequests);
        Assert.Same(world.Item, world.Room.GetRoomItemHandler().GetItem(7));
        Assert.False(world.Room.GetRoomUserManager().TryGetPet(12, out _));
        Assert.Null(world.Room.GetRoomItemHandler().GetItem(13));
    }

    [PetPlacementDatabaseFact]
    public void StoreCommitsPetAndPackageTogetherAndRollsBackExactMismatchOrSecondWriteFailure()
    {
        using var root = new MySqlConnection(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE"));
        root.Open();
        var schema = "gnome_" + Guid.NewGuid().ToString("N");
        root.Execute($"CREATE DATABASE `{schema}`");

        try {
            var cs = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE"))
            {
                Database = schema,
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true
            }.ConnectionString;
            using var connection = new MySqlConnection(cs);
            connection.Open();
            connection.Execute("""
                CREATE TABLE bots (id INT AUTO_INCREMENT PRIMARY KEY, user_id INT NOT NULL, room_id INT UNSIGNED NOT NULL,
                    name VARCHAR(50) NOT NULL, motto VARCHAR(50) NOT NULL, look VARCHAR(100) NOT NULL,
                    x INT NOT NULL, y INT NOT NULL, z DOUBLE NOT NULL, ai_type VARCHAR(20) NOT NULL) ENGINE=InnoDB;
                CREATE TABLE bots_petdata (id INT PRIMARY KEY, type INT NOT NULL, race VARCHAR(20) NOT NULL, color VARCHAR(20) NOT NULL,
                    experience INT NOT NULL, energy INT NOT NULL, nutrition INT NOT NULL, respect INT NOT NULL,
                    createstamp DATETIME(6) NULL, have_saddle INT NOT NULL, anyone_ride INT NOT NULL,
                    hairdye INT NOT NULL, pethair INT NOT NULL, gnome_clothing VARCHAR(100) NOT NULL) ENGINE=InnoDB;
                CREATE TABLE items (id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY, user_id INT NOT NULL, room_id INT UNSIGNED NOT NULL,
                    base_item INT UNSIGNED NOT NULL, x INT, y INT, z DOUBLE, wall_pos VARCHAR(128), rot INT, extra_data TEXT) ENGINE=InnoDB;
                INSERT INTO items (id,user_id,room_id,base_item) VALUES (7, 1, 42, 100);
                """);
            var store = new GnomePackageStore(new ProbeDatabase(cs), NullLogger<GnomePackageStore>.Instance);
            var instant = DateTimeOffset.Parse("2040-01-01T00:00:00.123456Z");
            var request = new GnomePackageRequest(7, 100, 1, "prepared owner", 42, 1, 1, 0.5, "Pixel", "hat", instant);

            foreach (var invalid in new[] { request with { OwnerId = 99 }, request with { RoomId = 99 }, request with { BaseItem = 101 } }) {
                Assert.Null(store.Open(invalid));
                Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
                Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots_petdata"));
                Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items"));
            }

            connection.Execute("CREATE TRIGGER fail_pet BEFORE INSERT ON bots_petdata FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='forced pet failure'");
            Assert.Null(store.Open(request));
            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items"));
            connection.Execute("DROP TRIGGER fail_pet");
            var pet = Assert.IsType<Pet>(store.Open(request));
            Assert.Equal(("prepared owner", 42u, instant, 0.5), (pet.OwnerName, pet.RoomId, pet.CreatedAt, pet.Z));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
            Assert.Equal(instant, connection.QuerySingle<DateTimeOffset>("SELECT createstamp FROM bots_petdata"));
            Assert.Empty(connection.Query<int>("SELECT id FROM items"));
            Assert.Null(store.Open(request));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));

            connection.Execute("INSERT INTO items (id,user_id,room_id,base_item) VALUES (8,1,42,100)");
            var package = new PetPackageRequest(8, 100, 1, "owner", 42, 1, 2, 1.5, "Smokey",
                new PetPackageFigure(1, 0, "ABABAB"), 1.6, 101, 4, instant);
            connection.Execute("CREATE TRIGGER fail_nest BEFORE INSERT ON items FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='nest insert failed'");
            Assert.Null(store.OpenPetPackage(package));
            Assert.Equal(8u, connection.QuerySingle<uint>("SELECT id FROM items"));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
            connection.Execute("DROP TRIGGER fail_nest");

            var openings = new PetPackageOpening?[2];
            Parallel.For(0, openings.Length, index => openings[index] = store.OpenPetPackage(package));

            var opened = Assert.Single(openings.OfType<PetPackageOpening>());
            Assert.Equal(opened.FurniId, connection.QuerySingle<uint>("SELECT id FROM items"));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items WHERE base_item=101 AND user_id=1 AND room_id=42 AND x=1 AND y=2 AND z=1.5 AND rot=4"));
            Assert.Equal(2, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots"));
            Assert.Equal(2, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM bots_petdata"));
        }
        finally {
            root.Execute($"DROP DATABASE `{schema}`");
        }
    }

    private static Fixture World(bool temporary = false, bool package = false, bool withNest = true, int nestWidth = 1, IBotAiFactory? botAi = null)
    {
        var model = new RoomModel("gnome-test", 0, 0, 0, 0, "000\r000\r000", 0, 0, false);
        var room = new Room(new RoomData { Id = 42, Model = model }, [], TestLogging.Navigation, TestLogging.Logger, TestRoomAchievements.Unused, TestRoomOwners.Unused);
        Set(room, "_interactionClock", TimeProvider.System);
        Set(room, "_roomItemHandling", new RoomItemHandling(room, TestRoomItemStore.Instance, TestRoomItemMetadataStore.Instance, TestGameClientManager.Empty, TestLanguageManager.RoomItems, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));
        Set(room, "_gamemap", new Gamemap(room, model, TestLogging.Navigation, TestRoomSettings.Empty, TestGroupManager.Empty, TestNavigationDatabase.Instance, TestNavigationRewards.Instance));
        Set(room, "_roomUserManager", new RoomUserManager(room, TestRoomUserStore.Instance, TimeProvider.System, new TestRewardProgress(), TestChatEmotions.Unused, botAi ?? TestBotAiFactory.Inert, TestGameClientManager.Empty, TestItemRuntime.Travel, TestItemRuntime.Settings));
        Set(room, "_userSnapshots", CatalogSnapshotTestSupport.Proxy<IRoomUserSnapshotService>((_, _) => null));
        var item = new Item
        {
            Id = 7,
            BaseItem = 100,
            OwnerId = 1,
            RoomId = 42,
            IsTemporary = temporary,
            Definition = package
                ? new() { Id = 100, ItemName = "newbie_cat_package", Type = ItemType.Floor, InteractionType = InteractionType.None, PetPackage = new(1, "newbie_nest") }
                : new() { Id = 100, Type = ItemType.Floor, InteractionType = InteractionType.GnomeBox },
            ExtraData = new LegacyDataFormat { Data = package ? "1 0 ABABAB" : "" }
        };
        item.SetState(1, 1, 0);
        item.Attach(room, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards);
        var floor = (ConcurrentDictionary<uint, Item>)typeof(RoomItemHandling).GetField("_floorItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(room.GetRoomItemHandler())!;
        floor.AddOrUpdate(7, item, (_, _) => item);
        room.GetGameMap().GenerateMaps();
        var (client, packets) = HabbiconTestSupport.Client(new Habbo
        {
            Id = 1,
            Username = "owner",
            CurrentRoom = room,
            Inventory = new InventoryComponent { Furniture = new FurnitureInventoryComponent([], []) }
        });
        var store = new RecordingStore();
        var clock = new CountingClock();
        var nest = new ItemDefinition { Id = 101, ItemName = "newbie_nest", Type = ItemType.Floor, Height = 0.1, Stackable = true, Width = nestWidth, Length = 1 };
        var known = withNest ? new Dictionary<uint, ItemDefinition> { [nest.Id] = nest } : new Dictionary<uint, ItemDefinition>();
        var definitions = CatalogSnapshotTestSupport.Proxy<IItemDataManager>((method, args) => method switch
        {
            "get_Items" => known,
            "GetItemByName" => known.Values.FirstOrDefault(definition => definition.ItemName == (string?)args[0]),
            _ => null
        });
        var factory = CatalogSnapshotTestSupport.Proxy<IItemFactory>((_, _) => throw new InvalidOperationException("No food definition supplied."));

        return new(room, item, client, packets, store, clock, new(store, definitions, factory, clock));
    }

    private static void Set(object instance, string field, object value) => instance.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);
    private sealed record Fixture(Room Room, Item Item, GameClient Client, List<(uint Header, byte[] Payload)> Packets,
        RecordingStore Store, CountingClock Clock, GnomePackageService Service);
    private sealed class RecordingService : IGnomePackageService
    {
        public (uint, string)? Request;
        public void Open(Room room, GameClient session, uint itemId, string petName) => Request = (itemId, petName);
    }
    private static byte[] Bytes(params object[] values) => HabbiconTestSupport.Incoming(values).Buffer.ToArray();

    private sealed class RecordingStore : IGnomePackageStore
    {
        public List<GnomePackageRequest> Requests { get; } = [];
        public List<PetPackageRequest> PetRequests { get; } = [];

        public PetPackageOpening? OpenPetPackage(PetPackageRequest request)
        {
            PetRequests.Add(request);

            return Fail ? null : new PetPackageOpening(new Pet(12, request.OwnerId, request.RoomId, request.Name, request.Figure.Type,
                request.Figure.Palette.ToString(), request.Figure.Colour, 0, 100, 100, 0, request.CreatedAt, request.X, request.Y, request.PetZ,
                0, 0, 0, -1, "-1", request.OwnerName), 13);
        }

        public Action<GnomePackageRequest>? Before;
        public bool Fail;
        public Pet? Open(GnomePackageRequest request)
        {
            Before?.Invoke(request);
            Requests.Add(request);

            return Fail ? null : new Pet(12, request.OwnerId, request.RoomId, request.Name, 26, "30", "ffffff", 0,
                100, 100, 0, request.CreatedAt, request.X, request.Y, request.Z, 0, 0, 0, -1, request.Clothing, request.OwnerName);
        }
    }
    private sealed class CountingClock : TimeProvider
    {
        public DateTimeOffset Now { get; } = DateTimeOffset.Parse("2040-01-01T00:00:00Z");
        public int Reads;
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;

            return Now;
        }
    }
    private sealed class ProbeDatabase(string cs) : IDatabase
    {
        public bool IsConnected() => true;
        public IDbConnection Connection() => new MySqlConnection(cs);
    }
}
