using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Plus.Communication.Flash;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Revisions;
using Plus.Database;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Badges;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Badges;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.HabboHotel.Permissions;
using Xunit;

namespace Plus.Tests;

/// <summary>Reward tracks against the real revision profiles and the claim write path.</summary>
public class RewardTrackLiveTests
{
    private const string Badge = "ACH_RewardTracksCompleted1";

    [Fact]
    public async Task LoginSendsTracksToTheVoltClient()
    {
        var client = new TestClient(await Profile());
        client.SetHabbo(new Habbo { Id = 7 });
        var manager = await Manager(new FakeDatabase(), new BadgeDefinitions());

        manager.SendTracks(client);

        Assert.Equal(new uint[] { ServerPacketHeader.RewardTracksComposer }, client.Sent);
    }

    [Fact]
    public async Task SendTracksReadsTheInjectedClockOnceAtANonUtcOffset()
    {
        var clock = new CountingClock(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.FromHours(5)));
        var client = new TestClient(await Profile());
        client.SetHabbo(new Habbo { Id = 7 });
        var manager = new RewardTrackManager(NullLogger<RewardTrackManager>.Instance, new FakeDatabase(), new BadgeDefinitions(), clock);
        await manager.Start();
        clock.Reads = 0;

        manager.SendTracks(client);

        Assert.Equal(1, clock.Reads);
        Assert.Equal(new uint[] { ServerPacketHeader.RewardTracksComposer }, client.Sent);
    }

    [Fact]
    public async Task BadgePrizeIsGrantedInTheClaimTransaction()
    {
        var database = TrackDatabase();
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(Badge));

        await manager.Claim(client, "introduction", "track_champ");

        var write = Assert.Single(database.Committed);
        Assert.Contains(write, sql => sql.Contains("INTO users_reward_track_prizes"));
        Assert.Contains(write, sql => sql.Contains("INTO user_badges"));
        Assert.True(Assert.IsType<InventoryComponent>(habbo.Inventory).Badges.HasBadge(Badge));
        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        Assert.True(client.Sent.IndexOf(ServerPacketHeader.RewardTrackClaimResultComposer) > client.Sent.IndexOf(ServerHeader(client, ServerPacketHeader.BadgesComposer)));
    }

    [Fact]
    public async Task PrizeWithoutABadgeDefinitionIsRejectedAndStaysClaimable()
    {
        var database = TrackDatabase();
        var badges = new BadgeDefinitions();
        var (client, habbo, manager) = await Claimer(database, badges);

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.False(Assert.IsType<InventoryComponent>(habbo.Inventory).Badges.HasBadge(Badge));

        badges.Add(Badge);
        await manager.Claim(client, "introduction", "track_champ");
        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        Assert.Single(database.Committed);
    }

    [Fact]
    public async Task BadgeLimitedToARightIsNotGrantedWithoutIt()
    {
        var database = TrackDatabase();
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions((Badge, "badge_staff")));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.False(Assert.IsType<InventoryComponent>(habbo.Inventory).Badges.HasBadge(Badge));
    }

    [Fact]
    public async Task FailedBadgeWriteCommitsNothingAndTheClaimCanBeRetried()
    {
        var database = TrackDatabase();
        database.FailOn = "INTO user_badges";
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(Badge));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.Equal(1, database.RolledBack);
        Assert.False(Assert.IsType<InventoryComponent>(habbo.Inventory).Badges.HasBadge(Badge));
        Assert.DoesNotContain(ServerHeader(client, ServerPacketHeader.BadgesComposer), client.Sent);

        database.FailOn = null;
        await manager.Claim(client, "introduction", "track_champ");
        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        Assert.True(habbo.Inventory.Badges.HasBadge(Badge));
    }

    [Fact]
    public async Task FurniPrizeIsDeliveredToTheInventoryInTheClaimTransaction()
    {
        var database = TrackDatabase(1, "1534", 2);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(), new FurniDefinitions(Sofa()));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        var write = Assert.Single(database.Committed);
        Assert.Contains(write, sql => sql.Contains("INTO users_reward_track_prizes"));
        Assert.Equal(2, write.Count(sql => sql.Contains("INSERT INTO `items`")));
        Assert.True(habbo.Inventory!.Furniture.HasItem(NewItemId));
        Assert.Contains(ServerHeader(client, ServerPacketHeader.FurniListNotificationComposer), client.Sent);
        Assert.Contains(ServerHeader(client, ServerPacketHeader.FurniListUpdateComposer), client.Sent);
    }

    [Fact]
    public async Task WallFurniPrizeIsDeliveredByItsWallSprite()
    {
        var database = TrackDatabase(0, "4001", 1);
        var poster = new ItemDefinition { Id = 78, SpriteId = 4001, ItemName = "poster", Type = ItemType.Wall, ProductType = "i" };
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(), new FurniDefinitions(Sofa(), poster));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        Assert.Equal(78u, habbo.Inventory!.Furniture.Wall[NewItemId].Definition.Id);
    }

    [Fact]
    public async Task FurniPrizeWithoutADefinitionIsUnknownAndStoresNothing()
    {
        var database = TrackDatabase(1, "9999", 1);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(), new FurniDefinitions(Sofa()));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.Empty(habbo.Inventory!.Furniture.AllItems);
    }

    [Fact]
    public async Task FailedFurniWriteStoresNoClaimAndTheRetryGrantsOnce()
    {
        var database = TrackDatabase(1, "1534", 1);
        database.FailOn = "INSERT INTO `items`";
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(), new FurniDefinitions(Sofa()));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.Equal(1, database.RolledBack);
        Assert.Empty(habbo.Inventory!.Furniture.AllItems);

        database.FailOn = null;
        await manager.Claim(client, "introduction", "track_champ");
        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());

        await manager.Claim(client, "introduction", "track_champ");
        Assert.Equal(RewardTrackResults.AlreadyClaimed, client.ClaimResult());
        var write = Assert.Single(database.Committed);
        Assert.Single(write, sql => sql.Contains("INSERT INTO `items`"));
    }

    [Fact]
    public async Task ActivityPointPrizeAddsItsCurrencyInTheClaimTransaction()
    {
        var database = TrackDatabase(8, "104", 100);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions());

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        var write = Assert.Single(database.Committed);
        Assert.Contains(write, sql => sql.Contains("INTO users_reward_track_prizes"));
        Assert.Contains(write, sql => sql.Contains("INTO user_currencies"));
        Assert.Equal(100, habbo.Currencies[104]);
        Assert.Contains(ServerHeader(client, ServerPacketHeader.HabboActivityPointNotificationComposer), client.Sent);
    }

    [Fact]
    public async Task CreditPrizeAddsCreditsInTheClaimTransaction()
    {
        var database = TrackDatabase(8, "-1", 5);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions());

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        Assert.Contains(Assert.Single(database.Committed), sql => sql.Contains("UPDATE users SET credits"));
        Assert.Equal(5, habbo.Credits);
        Assert.Contains(ServerHeader(client, ServerPacketHeader.CreditBalanceComposer), client.Sent);
    }

    [Fact]
    public async Task PetPrizeDeliversTheNewestPackageOfItsPetTypeHoldingThePrizeFigure()
    {
        var database = TrackDatabase(10, "1", 1, "1 0 ABABAB");
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(), new FurniDefinitions(Sofa(), Nest(),
            Package(80, "newbie_present", 45508, 1),
            Package(81, "newbie_cat_package", 75424, 1),
            Package(82, "newbie_crocodile_package", 75424, 2),
            Package(83, "newbie_cat_package_copy", 75424, 1)));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        var write = Assert.Single(database.Committed);
        Assert.Contains(write, sql => sql.Contains("INTO users_reward_track_prizes"));
        Assert.Single(write, sql => sql.Contains("INSERT INTO `items`"));
        var package = habbo.Inventory!.Furniture.Floor[NewItemId];
        Assert.Equal(81u, package.Definition.Id);
        Assert.Equal("1 0 ABABAB", package.ExtraData.Serialize());
        Assert.Contains(ServerHeader(client, ServerPacketHeader.FurniListNotificationComposer), client.Sent);
        Assert.Contains(ServerHeader(client, ServerPacketHeader.FurniListUpdateComposer), client.Sent);
    }

    [Fact]
    public async Task PetPrizeWithoutAPackageForItsPetTypeIsUnknownAndStoresNothing()
    {
        var database = TrackDatabase(10, "3", 1, "3 0 FFFFFF");
        var withoutItsFurni = new ItemDefinition
        {
            Id = 84,
            SpriteId = 55842,
            ItemName = "horse_dye_17",
            Type = ItemType.Floor,
            ProductType = "s",
            PetPackage = new PetPackage(3, "Lime")
        };
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions(),
            new FurniDefinitions(Sofa(), Nest(), Package(81, "newbie_cat_package", 75424, 1), withoutItsFurni));

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.Empty(habbo.Inventory!.Furniture.AllItems);
    }

    [Fact]
    public async Task BotPrizeDeliversTheBotOfThePresetWithItsFigure()
    {
        var database = TrackDatabase(6, "GENERIC", 1, RobbieFigure);
        database.Tables["FROM catalog_bot_presets"] = Rows(["Id", "Name", "Figure", "Gender", "Motto", "AiType"],
            [3570u, "Skye", "hr-9534-39.hd-600-1", "F", "Serves drinks", "bartender"],
            [3568u, "Frank", "hd-180-1", "M", "Another generic bot", "generic"],
            [3569u, "Robbie", RobbieFigure, "M", "Can talk, walk, dance and dress up", "generic"]);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions());

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        var write = Assert.Single(database.Committed);
        Assert.Contains(write, sql => sql.Contains("INTO users_reward_track_prizes"));
        Assert.Single(write, sql => sql.Contains("INSERT INTO bots"));
        var bot = Assert.Single(habbo.Inventory!.Bots.Bots.Values);
        Assert.Equal((int)NewItemId, bot.Id);
        Assert.Equal(("Robbie", "M", RobbieFigure, "Can talk, walk, dance and dress up"), (bot.Name, bot.Gender, bot.Figure, bot.Motto));
        Assert.Contains(ServerHeader(client, ServerPacketHeader.BotInventoryComposer), client.Sent);
        Assert.Contains(ServerHeader(client, ServerPacketHeader.FurniListNotificationComposer), client.Sent);
    }

    [Fact]
    public async Task BotPrizeWithAnUnknownFigureTakesTheFirstPresetOfItsAiType()
    {
        var database = TrackDatabase(6, "GENERIC", 1, "hd-190-1.ch-210-66");
        database.Tables["FROM catalog_bot_presets"] = Rows(["Id", "Name", "Figure", "Gender", "Motto", "AiType"],
            [3570u, "Skye", "hr-9534-39.hd-600-1", "F", "Serves drinks", "bartender"],
            [3569u, "Robbie", RobbieFigure, "M", "Can talk, walk, dance and dress up", "generic"],
            [3571u, "Frank", "hd-180-1", "M", "Another generic bot", "generic"]);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions());

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Ok, client.ClaimResult());
        var bot = Assert.Single(habbo.Inventory!.Bots.Bots.Values);
        Assert.Equal(("Robbie", "M", "hd-190-1.ch-210-66"), (bot.Name, bot.Gender, bot.Figure));
    }

    [Fact]
    public async Task BotPrizeWithoutAPresetOfItsAiTypeIsUnknownAndStoresNothing()
    {
        var database = TrackDatabase(6, "GENERIC", 1, RobbieFigure);
        database.Tables["FROM catalog_bot_presets"] = Rows(["Id", "Name", "Figure", "Gender", "Motto", "AiType"],
            [3570u, "Skye", "hr-9534-39.hd-600-1", "F", "Serves drinks", "bartender"]);
        var (client, habbo, manager) = await Claimer(database, new BadgeDefinitions());

        await manager.Claim(client, "introduction", "track_champ");

        Assert.Equal(RewardTrackResults.Unknown, client.ClaimResult());
        Assert.Empty(database.Committed);
        Assert.Empty(habbo.Inventory!.Bots.Bots);
    }

    private const uint NewItemId = 9001;

    private const string RobbieFigure = "hr-3020-34.hd-3091-2.ch-225-92.lg-3058-100.sh-3089-1338.ca-3084-78-108.wa-2005";

    private static ItemDefinition Nest() => new() { Id = 79, SpriteId = 3897, ItemName = "newbie_nest", Type = ItemType.Floor, ProductType = "s" };

    private static ItemDefinition Package(uint id, string name, int revision, int petType) => new()
    {
        Id = id,
        SpriteId = (int)id + 18000,
        ItemName = name,
        Type = ItemType.Floor,
        ProductType = "s",
        Revision = revision,
        PetPackage = new PetPackage(petType, "newbie_nest")
    };

    private static ItemDefinition Sofa() => new() { Id = 77, SpriteId = 1534, ItemName = "club_sofa", Type = ItemType.Floor, ProductType = "s" };

    private static uint ServerHeader(TestClient client, uint internalId) => Assert.IsType<Plus.Communication.Revisions.Revision>(client.Revision).InternalIdToOutgoingIdMapping[internalId];

    private static Task<Revision> Profile() => Task.FromResult(new RevisionsCache().InternalRevision);

    private static async Task<RewardTrackManager> Manager(FakeDatabase database, IBadgeManager badges, IItemDataManager? items = null)
    {
        var manager = new RewardTrackManager(NullLogger<RewardTrackManager>.Instance, database, badges, new FixedTimeProvider(FixedTimeProvider.Epoch),
            items: items, itemFactory: new ItemFactory(database));
        await manager.Start();

        return manager;
    }

    private static async Task<(TestClient Client, Habbo Habbo, RewardTrackManager Manager)> Claimer(FakeDatabase database, IBadgeManager badges,
        IItemDataManager? items = null)
    {
        var client = new TestClient(await Profile());
        var habbo = new Habbo
        {
            Id = 7,
            Username = "player",
            Access = UserAccess.Empty,
            Inventory = new InventoryComponent { Badges = new BadgesInventoryComponent(new()), Furniture = new FurnitureInventoryComponent([], []) }
        };
        client.SetHabbo(habbo);

        return (client, habbo, await Manager(database, badges, items));
    }

    /// <summary>The introduction track's champ prize, with the player 10 points past it.</summary>
    private static FakeDatabase TrackDatabase() => TrackDatabase(4, Badge, 1);

    /// <summary>A track with one prize named track_champ, worth 50 points, with the player 10 points past it.</summary>
    private static FakeDatabase TrackDatabase(int productType, string rewardType, int amount, string extraParams = "")
    {
        var database = new FakeDatabase();
        database.Tables["FROM reward_tracks "] = Table(
            ("Id", "introduction"), ("Theme", "blue"), ("SortOrder", 0), ("StartsAt", DBNull.Value), ("EndsAt", DBNull.Value), ("HasPremium", 1),
            ("Boost", 1.0), ("InstantPoints", 0), ("CostDiamonds", 0), ("CostCredits", 25));
        database.Tables["FROM reward_track_prizes"] = Table(
            ("TrackId", "introduction"), ("Id", "track_champ"), ("RequiredPoints", 50), ("ProductItemTypeId", productType), ("RewardType", rewardType),
            ("ExtraParams", extraParams), ("RewardAmount", amount), ("Premium", 0), ("SortOrder", 1));
        database.Tables["LAST_INSERT_ID"] = Table(("Id", NewItemId));
        database.Tables["FROM users_reward_tracks "] = Table(("TrackId", "introduction"), ("Points", 60), ("Premium", 0));

        return database;
    }

    private static DataTable Table(params (string Column, object Value)[] row)
    {
        var table = new DataTable();

        foreach (var (column, value) in row) {
            table.Columns.Add(column, value is DBNull ? typeof(DateTimeOffset) : value.GetType());
        }

        table.Rows.Add(row.Select(cell => cell.Value).ToArray());

        return table;
    }

    private static DataTable Rows(string[] columns, params object[][] rows)
    {
        var table = new DataTable();

        for (var i = 0; i < columns.Length; i++) {
            table.Columns.Add(columns[i], rows[0][i].GetType());
        }

        foreach (var row in rows) {
            table.Rows.Add(row);
        }

        return table;
    }

    private sealed class FurniDefinitions(params ItemDefinition[] definitions) : IItemDataManager
    {
        public Dictionary<uint, ItemDefinition> Items { get; } = definitions.ToDictionary(definition => definition.Id);
        public Dictionary<int, uint> Gifts { get; } = new();
        public void Init() { }
        public ItemDefinition? GetItemByName(string name) => Items.Values.FirstOrDefault(definition => definition.ItemName == name);
    }

    private sealed class BadgeDefinitions : IBadgeManager
    {
        private readonly Dictionary<string, BadgeDefinition> _badges = new();

        public BadgeDefinitions(params string[] codes)
        {
            foreach (var code in codes) {
                Add(code);
            }
        }

        public BadgeDefinitions((string Code, string Right) badge) =>
            _badges[badge.Code.ToUpper()] = new() { Code = badge.Code, RequiredRight = badge.Right };

        public IReadOnlyDictionary<string, BadgeDefinition> Badges => _badges;
        public void Add(string code) => _badges[code.ToUpper()] = new() { Code = code };
        public Task Init() => Task.CompletedTask;
        public Task GiveBadge(Habbo habbo, string code) => throw new InvalidOperationException("Claims grant badges in their own transaction.");
        public bool StoreBadge(IDbConnection connection, IDbTransaction transaction, Habbo habbo, string code) => throw new NotSupportedException();
        public void PublishBadge(Habbo habbo, string code) => throw new NotSupportedException();
        public Task RemoveBadge(Habbo habbo, string badge) => throw new NotSupportedException();
        public Task<bool> ReplaceBadge(Habbo habbo, string? previous, string code) => throw new NotSupportedException();
        public Task<List<Badge>> LoadBadgesForHabbo(int userId) => throw new NotSupportedException();
    }

    /// <summary>Serves canned SELECT results and records each committed transaction's statements.</summary>
    private sealed class CountingClock(DateTimeOffset now) : TimeProvider
    {
        public int Reads { get; set; }
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;

            return now;
        }
    }

    private sealed class FakeDatabase : IDatabase
    {
        public Dictionary<string, DataTable> Tables { get; } = new();
        public List<List<string>> Committed { get; } = new();
        public int RolledBack { get; set; }
        public string? FailOn { get; set; }
        public bool IsConnected() => true;
        public IDbConnection Connection() => new FakeConnection(this);

        public DataTable Select(string sql) => Tables.FirstOrDefault(entry => sql.Contains(entry.Key)).Value?.Copy() ?? new DataTable();
    }

    private sealed class FakeConnection(FakeDatabase database) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;
        public FakeDatabase Store => database;
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "";
        public override string DataSource => "";
        public override string ServerVersion => "";
        public override ConnectionState State => _state;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeTransaction(this);
        protected override DbCommand CreateDbCommand() => new FakeCommand { Connection = this };
    }

    private sealed class FakeTransaction(FakeConnection connection) : DbTransaction
    {
        private bool _done;
        public List<string> Statements { get; } = new();
        public override IsolationLevel IsolationLevel => IsolationLevel.RepeatableRead;
        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {
            connection.Store.Committed.Add(Statements);
            _done = true;
        }

        public override void Rollback()
        {
            if (_done) {
                return;
            }

            connection.Store.RolledBack++;
            _done = true;
        }

        protected override void Dispose(bool disposing)
        {
            Rollback();
            base.Dispose(disposing);
        }
    }

    private sealed class FakeCommand : DbCommand
    {
        [AllowNull] public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameters();
        protected override DbTransaction? DbTransaction { get; set; }
        private FakeDatabase Owner => ((FakeConnection)DbConnection!).Store;
        public override void Cancel() { }
        public override void Prepare() { }
        public override object? ExecuteScalar() => null;
        protected override DbParameter CreateDbParameter() => new FakeParameter();

        public override int ExecuteNonQuery()
        {
            if (Owner.FailOn != null && CommandText.Contains(Owner.FailOn)) {
                throw new InvalidOperationException("Injected write failure.");
            }

            if (DbTransaction is FakeTransaction transaction) {
                transaction.Statements.Add(CommandText);
            }
            else {
                Owner.Committed.Add(new() { CommandText });
            }

            return 1;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => Owner.Select(CommandText).CreateDataReader();
    }

    private sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull] public override string ParameterName { get; set; } = "";
        public override int Size { get; set; }
        [AllowNull] public override string SourceColumn { get; set; } = "";
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeParameters : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];
        public override int Count => _items.Count;
        public override object SyncRoot => _items;
        public override int Add(object value)
        {
            _items.Add((DbParameter)value);

            return _items.Count - 1;
        }
        public override void AddRange(Array values)
        {
            foreach (var value in values) {
                Add(value);
            }
        }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_items).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }

    /// <summary>A Flash client on a real revision profile; records outgoing headers and bodies.</summary>
    private sealed class TestClient : GameClient
    {
        private readonly List<byte[]> _bodies = new();
        public List<uint> Sent { get; } = new();

        public TestClient(Revision revision) : base(TestGameServer.Instance, new FlashPacketFactory(), TestLogging.GameClient)
        {
            Revision = revision;
            SendCallback = args =>
            {
                _bodies.Add(args.MemoryBuffer.Slice(6).ToArray());

                return false;
            };
        }

        /// <summary>Result code of the last RewardTrackClaimResult: string track, string prize, int result.</summary>
        public RewardTrackResults ClaimResult()
        {
            var body = _bodies[Sent.LastIndexOf(ServerPacketHeader.RewardTrackClaimResultComposer)];

            return (RewardTrackResults)BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(body.Length - 4));
        }

        internal override (bool Complete, bool Malformed, uint MessageId, int HeaderLength, int Length) GetMessageIdAndPacketLength(ReadOnlyMemory<byte> buffer) =>
            (true, false, 0, 0, 0);

        public override void CreateHeader(Memory<byte> memory, uint messageId) => Sent.Add(messageId);
    }
}
