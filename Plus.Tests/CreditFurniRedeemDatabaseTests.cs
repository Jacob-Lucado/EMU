using System.Data;
using Dapper;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

// A redeemed credit furni and its credits change together: either the item is gone and the wallet row holds its value, or neither changed.
public sealed class CreditFurniRedeemDatabaseTests
{
    private const int Owner = 7;
    private const uint Room = 42;

    [RoomComponentDatabaseFact]
    public void RedeemingACoinDeletesItAndPaysItsCreditsInOneStep()
    {
        WithSchema((connection, database) =>
        {
            new ItemRedemptionStore(database).RedeemExchange(50, Owner, Room, 10);

            Assert.False(Exists(connection, 50));
            Assert.Equal(110, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void AFailedWalletWriteKeepsTheCoinAndPaysNothing()
    {
        WithSchema((connection, database) =>
        {
            FailWalletWrites(connection);

            Assert.ThrowsAny<Exception>(() => new ItemRedemptionStore(database).RedeemExchange(50, Owner, Room, 10));

            Assert.True(Exists(connection, 50));
            Assert.Equal(100, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void ACoinOfAnotherOwnerOrRoomPaysNothing()
    {
        WithSchema((connection, database) =>
        {
            var store = new ItemRedemptionStore(database);

            Assert.Throws<InvalidOperationException>(() => store.RedeemExchange(50, 8, Room, 10));
            Assert.Throws<InvalidOperationException>(() => store.RedeemExchange(50, Owner, 43, 10));

            Assert.True(Exists(connection, 50));
            Assert.Equal(100, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void TwoRedeemsOfTheSameCoinPayOnce()
    {
        WithSchema((connection, database) =>
        {
            var store = new ItemRedemptionStore(database);
            var outcomes = Race(database, [50], () => store.RedeemExchange(50, Owner, Room, 10), () => store.RedeemExchange(50, Owner, Room, 10));

            Assert.Equal(1, outcomes.Count(outcome => outcome == null));
            Assert.IsType<InvalidOperationException>(Assert.Single(outcomes, outcome => outcome != null));
            Assert.False(Exists(connection, 50));
            Assert.Equal(110, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void ConvertingTheInventoryPaysEveryRedeemedItemInOneStep()
    {
        WithSchema((connection, database) =>
        {
            var redeemed = new ItemRedemptionStore(database).RedeemInventoryExchanges(Owner, Inventory);

            Assert.Equal([60u, 61u], redeemed.Order());
            Assert.False(Exists(connection, 60));
            Assert.False(Exists(connection, 61));
            Assert.True(Exists(connection, 62));
            Assert.True(Exists(connection, 63));
            Assert.Equal(115, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void AFailedWalletWriteDuringAConvertKeepsEveryItem()
    {
        WithSchema((connection, database) =>
        {
            FailWalletWrites(connection);

            Assert.ThrowsAny<Exception>(() => new ItemRedemptionStore(database).RedeemInventoryExchanges(Owner, Inventory));

            Assert.True(Exists(connection, 60));
            Assert.True(Exists(connection, 61));
            Assert.Equal(100, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void TwoConvertsOfTheSameItemsPayOnce()
    {
        WithSchema((connection, database) =>
        {
            var store = new ItemRedemptionStore(database);
            IReadOnlyList<uint>? first = null, second = null;
            var outcomes = Race(database, [60, 61],
                () => first = store.RedeemInventoryExchanges(Owner, Inventory),
                () => second = store.RedeemInventoryExchanges(Owner, Inventory));

            Assert.All(outcomes, Assert.Null);
            Assert.Equal([60u, 61u], first!.Concat(second!).Order());
            Assert.Equal(115, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void ATradedVoucherIsDeletedAndPaidInOneStep()
    {
        WithSchema((connection, database) =>
        {
            Trades(database).RedeemItem(70, Owner, 10);

            Assert.False(Exists(connection, 70));
            Assert.Equal(110, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void AFailedWalletWriteKeepsTheTradedVoucher()
    {
        WithSchema((connection, database) =>
        {
            FailWalletWrites(connection);

            Assert.ThrowsAny<Exception>(() => Trades(database).RedeemItem(70, Owner, 10));

            Assert.True(Exists(connection, 70));
            Assert.Equal(100, Credits(connection));
        });
    }

    [RoomComponentDatabaseFact]
    public void ATradedVoucherThatIsGonePaysNothing()
    {
        WithSchema((connection, database) =>
        {
            Assert.Throws<InvalidOperationException>(() => Trades(database).RedeemItem(99, Owner, 10));

            Assert.Equal(100, Credits(connection));
        });
    }

    private static readonly ExchangeRedemption[] Inventory = [new(60, 10), new(61, 5), new(62, 20), new(63, 50), new(64, 1)];

    private static ITradeStore Trades(IDatabase database) => new RoomTradingComponent(database, TimeProvider.System, TestRoomSettings.Empty);

    private static bool Exists(IDbConnection connection, uint id) => connection.ExecuteScalar<int>("SELECT COUNT(*) FROM items WHERE id=@id", new { id }) == 1;

    private static int Credits(IDbConnection connection) => connection.ExecuteScalar<int>("SELECT credits FROM users WHERE id=@Owner", new { Owner });

    private static void FailWalletWrites(IDbConnection connection) =>
        connection.Execute("CREATE TRIGGER wallet_write_fails BEFORE UPDATE ON users FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='wallet write failed'");

    // Holds the item rows until both redeems wait on them, so the two run at the same time.
    private static Exception?[] Race(IDatabase database, uint[] ids, Action first, Action second)
    {
        using var connection = database.Connection();
        using var holder = database.Connection();
        holder.Open();
        using var hold = holder.BeginTransaction();
        holder.Query<uint>("SELECT id FROM items WHERE id IN @ids FOR UPDATE", new { ids }, hold).AsList();
        var tasks = new[] { first, second }.Select(action => Task.Run(() =>
        {
            try {
                action();

                return null;
            }
            catch (Exception exception) {
                return exception;
            }
        })).ToArray();
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline && connection.ExecuteScalar<int>(
                   "SELECT COUNT(*) FROM information_schema.PROCESSLIST WHERE DB=DATABASE() AND COMMAND='Query' AND INFO LIKE 'DELETE FROM items%'") < 2) {
            Thread.Sleep(20);
        }

        hold.Commit();

        return Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    private static void WithSchema(Action<IDbConnection, IDatabase> test)
    {
        var root = Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!;
        var schema = "credit_redeem_" + Guid.NewGuid().ToString("N");
        using var server = new MySqlConnection(root);
        server.Execute($"CREATE DATABASE `{schema}`");

        try {
            var database = new SchemaDatabase(new MySqlConnectionStringBuilder(root) { Database = schema }.ConnectionString);
            using var connection = database.Connection();
            connection.Execute("""
                CREATE TABLE items (id INT UNSIGNED PRIMARY KEY, user_id INT NOT NULL, room_id INT UNSIGNED NOT NULL DEFAULT 0) ENGINE=InnoDB;
                CREATE TABLE users (id INT PRIMARY KEY, credits INT NOT NULL) ENGINE=InnoDB;
                INSERT INTO users VALUES (7, 100), (8, 100);
                INSERT INTO items (id, user_id, room_id) VALUES (50, 7, 42), (60, 7, 0), (61, 7, 0), (62, 7, 42), (63, 8, 0), (70, 9, 0);
                """);
            test(connection, database);
        }
        finally {
            server.Execute($"DROP DATABASE `{schema}`");
        }
    }

    private sealed class SchemaDatabase(string connectionString) : IDatabase
    {
        public bool IsConnected() => true;
        public IDbConnection Connection() => new MySqlConnection(connectionString);
    }
}
