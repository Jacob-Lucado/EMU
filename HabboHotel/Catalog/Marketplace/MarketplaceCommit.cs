using System.Data;
using Dapper;
using Plus.Database;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Catalog.Marketplace;

internal sealed class MarketplaceOutcomeUnknownException(Exception cause) : Exception("Marketplace outcome requires a fresh session.", cause);
internal sealed class MarketplaceCommitFailedException(Exception cause) : Exception("Marketplace operation was rolled back.", cause);

internal static class MarketplaceCommit
{
    public static T Guard<T>(Habbo habbo, Func<T> action)
    {
        try {
            return action();
        }
        catch (MarketplaceOutcomeUnknownException) {
            habbo.CurrencyPersistenceUncertain = true;
            habbo.Client?.Disconnect();
            throw;
        }
    }

    // A rolled-back operation changed nothing, so the client gets the same answer as for a refusal.
    public static T Guard<T>(Habbo habbo, Func<T> action, T rolledBack)
    {
        try {
            return Guard(habbo, action);
        }
        catch (MarketplaceCommitFailedException) {
            return rolledBack;
        }
    }

    public static void LockUser(IDbConnection connection, IDbTransaction transaction, int userId)
    {
        if (connection.QuerySingleOrDefault<int?>("SELECT id FROM users WHERE id=@userId FOR UPDATE", new { userId }, transaction) == null) {
            throw new InvalidOperationException("Marketplace user no longer exists.");
        }
    }

    public static void Complete(IDatabase database, IDbConnection connection, IDbTransaction transaction, int userId, string kind)
    {
        var reference = Guid.NewGuid().ToString();
        // An operation row is only read right after its own commit, so a row older than a day is never needed again.
        connection.Execute("INSERT INTO marketplace_operations(operation_ref,user_id,kind,created_at) VALUES(@reference,@userId,@kind,UTC_TIMESTAMP(6));" +
            "DELETE FROM marketplace_operations WHERE user_id=@userId AND created_at<UTC_TIMESTAMP(6) - INTERVAL 1 DAY", new { reference, userId, kind }, transaction);

        try {
            transaction.Commit();
        }
        // Any commit error can come after the server committed, so every one is checked against the operation row.
        catch (Exception error) {
            Rollback(transaction);

            try {
                connection.Close();
            }
            catch (Exception) { }

            bool committed;

            try {
                using var recovery = database.Connection();
                recovery.Open();
                using var recovered = recovery.BeginTransaction();
                LockUser(recovery, recovered, userId);
                committed = recovery.ExecuteScalar<int>("SELECT COUNT(*) FROM marketplace_operations WHERE operation_ref=@reference AND user_id=@userId AND kind=@kind",
                    new { reference, userId, kind }, recovered) == 1;
            }
            catch (Exception recoveryError) {
                throw new MarketplaceOutcomeUnknownException(new AggregateException(error, recoveryError));
            }

            if (!committed) {
                throw new MarketplaceCommitFailedException(error);
            }
        }
    }

    public static void Rollback(IDbTransaction transaction)
    {
        try {
            transaction.Rollback();
        }
        catch (Exception) { }
    }
}
