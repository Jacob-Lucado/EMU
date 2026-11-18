using Plus.HabboHotel.Catalog.Marketplace;
using Dapper;
using Plus.Database;
using Plus.Core.Settings;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Rooms;

internal interface ITradeStore
{
    void DeleteItem(uint itemId);
    void RedeemItem(uint itemId, uint senderId, int recipientId, int balance);
    void TransferItem(uint itemId, uint senderId, int userId);
    void Log(int firstUserId, int secondUserId, string firstItems, string secondItems);
    bool Commit(IReadOnlyList<TradeTransfer> transfers, int firstUserId, int secondUserId, int firstCredits, int secondCredits,
        string firstItems, string secondItems, Func<bool> apply);
}

internal sealed record TradeTransfer(uint ItemId, int SenderId, int RecipientId, bool Redeem);

public sealed class RoomTradingComponent(IDatabase database, TimeProvider clock, ISettingsManager settings) : IRoomComponent, ITradeStore
{
    public int Order => 10;
    public void Initiate(Room room)
    {
        room.SetTrading(new TradingComponent(room, this, settings));
    }
    public void Initiated() { }

    void ITradeStore.DeleteItem(uint itemId)
    {
        using var connection = database.Connection();
        connection.Execute("DELETE FROM items WHERE id = @itemId LIMIT 1", new { itemId });
    }

    void ITradeStore.RedeemItem(uint itemId, uint senderId, int recipientId, int balance)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        if (connection.Execute("DELETE FROM items WHERE id = @itemId AND user_id = @senderId AND room_id = 0 LIMIT 1", new { itemId, senderId }, transaction) != 1) {
            throw new InvalidOperationException("Traded credit furniture was not deleted.");
        }

        ItemRedemptionStore.SaveBalance(connection, transaction, recipientId, balance);
        MarketplaceCommit.Complete(database, connection, transaction, recipientId, "exchange");
    }

    void ITradeStore.TransferItem(uint itemId, uint senderId, int userId)
    {
        using var connection = database.Connection();

        if (connection.Execute("UPDATE items SET user_id = @userId WHERE id = @itemId AND user_id = @senderId AND room_id = 0 LIMIT 1",
                new { userId, itemId, senderId }) != 1) {
            throw new InvalidOperationException("Traded furniture is no longer available.");
        }
    }

    void ITradeStore.Log(int firstUserId, int secondUserId, string firstItems, string secondItems)
    {
        using var connection = database.Connection();
        var now = clock.GetUtcNow();
        connection.Execute("""
            INSERT INTO logs_client_trade (`1id`, `2id`, `1items`, `2items`, `timestamp`)
            VALUES (@firstUserId, @secondUserId, @firstItems, @secondItems, @createdAt)
            """, new { firstUserId, secondUserId, firstItems, secondItems, createdAt = now.UtcDateTime });
    }

    bool ITradeStore.Commit(IReadOnlyList<TradeTransfer> transfers, int firstUserId, int secondUserId, int firstCredits, int secondCredits,
        string firstItems, string secondItems, Func<bool> apply)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try {
            foreach (var transfer in transfers) {
                var changed = transfer.Redeem
                    ? connection.Execute("DELETE FROM items WHERE id = @ItemId AND user_id = @SenderId AND room_id = 0 LIMIT 1", transfer, transaction)
                    : connection.Execute("UPDATE items SET user_id = @RecipientId WHERE id = @ItemId AND user_id = @SenderId AND room_id = 0 LIMIT 1", transfer, transaction);

                if (changed != 1) {
                    transaction.Rollback();

                    return false;
                }
            }

            if (connection.Execute("UPDATE users SET credits = @firstCredits WHERE id = @firstUserId LIMIT 1", new { firstCredits, firstUserId }, transaction) != 1 ||
                connection.Execute("UPDATE users SET credits = @secondCredits WHERE id = @secondUserId LIMIT 1", new { secondCredits, secondUserId }, transaction) != 1) {
                transaction.Rollback();

                return false;
            }

            var now = clock.GetUtcNow();

            if (connection.Execute("""
                INSERT INTO logs_client_trade (`1id`, `2id`, `1items`, `2items`, `timestamp`)
                VALUES (@firstUserId, @secondUserId, @firstItems, @secondItems, @createdAt)
                """, new { firstUserId, secondUserId, firstItems, secondItems, createdAt = now.UtcDateTime }, transaction) != 1 || !apply()) {
                transaction.Rollback();

                return false;
            }

            if (transfers.Any(transfer => transfer.Redeem)) {
                MarketplaceCommit.Complete(database, connection, transaction, firstUserId, "trade");
            }
            else {
                transaction.Commit();
            }

            return true;
        }
        catch {
            MarketplaceCommit.Rollback(transaction);
            throw;
        }
    }
}
