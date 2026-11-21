using Dapper;
using MySqlConnector;
using Plus.HabboHotel.Groups;
using Xunit;

namespace Plus.Tests;

/// <summary>Migration 171 and the invitation store on a real MariaDB schema built from the fresh install.</summary>
public class GroupInvitationDatabaseTests
{
    private const string Migration = "Database/Migrations/171_GroupInvitations.sql";

    [Fact]
    public void FreshInstallCarriesTheInvitationTable()
    {
        var fresh = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));

        Assert.Contains("CREATE TABLE `group_invitations` (", fresh);
        Assert.Contains("CREATE TABLE IF NOT EXISTS `group_invitations` (", File.ReadAllText(HabbiconPacketTests.Repo(Migration)));
    }

    [GroupParticipationDatabaseFact]
    public async Task MigrationCreatesTheTableOnceAndARerunChangesNothing()
    {
        await WithSchema(async (connection, _) =>
        {
            connection.Execute("INSERT INTO group_invitations (group_id, user_id, inviter_id, room_id, created_at) VALUES (1, 7, 1, 5, UTC_TIMESTAMP(6))");
            connection.Execute(File.ReadAllText(HabbiconPacketTests.Repo(Migration)));

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_invitations"));
            Assert.Equal("utf8mb4_unicode_ci", connection.ExecuteScalar<string>(
                "SELECT TABLE_COLLATION FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'group_invitations'"));
            Assert.Equal(new[] { "group_id", "user_id" }, connection.Query<string>(
                "SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'group_invitations' " +
                "AND CONSTRAINT_NAME = 'PRIMARY' ORDER BY ORDINAL_POSITION"));
            await Task.CompletedTask;
        });
    }

    [GroupParticipationDatabaseFact]
    public async Task SaveRefreshesOnePendingInvitationAndDeleteRemovesIt()
    {
        await WithSchema(async (connection, store) =>
        {
            var first = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
            store.Save(new GroupInvitation(1, 7, 1, 5, first));
            store.Save(new GroupInvitation(1, 7, 9, 6, first.AddMinutes(1)));

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_invitations"));
            Assert.Equal(new GroupInvitation(1, 7, 9, 6, first.AddMinutes(1)), store.Find(1, 7));
            Assert.Null(store.Find(2, 7));

            Assert.True(store.Delete(1, 7));
            Assert.False(store.Delete(1, 7));
            Assert.Null(store.Find(1, 7));
            await Task.CompletedTask;
        });
    }

    [GroupParticipationDatabaseFact]
    public async Task AcceptTurnsTheInvitationIntoAMembershipAndSettlesARequest()
    {
        await WithSchema(async (connection, store) =>
        {
            connection.Execute("INSERT INTO group_requests (group_id, user_id) VALUES (2, 7)");
            store.Save(new GroupInvitation(2, 7, 1, 5, DateTimeOffset.UtcNow));

            Assert.Equal(GroupJoinOutcome.Inserted, store.Accept(7, 2, 1500));

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_memberships WHERE group_id = 2 AND user_id = 7 AND `rank` = 0"));
            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_requests"));
            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_invitations"));

            Assert.Equal(GroupJoinOutcome.Refused, store.Accept(7, 2, 1500));
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_memberships"));
            await Task.CompletedTask;
        });
    }

    [GroupParticipationDatabaseFact]
    public async Task AcceptWithoutInvitationAtTheLimitOrAsMemberWritesNoMembership()
    {
        await WithSchema(async (connection, store) =>
        {
            Assert.Equal(GroupJoinOutcome.Refused, store.Accept(7, 1, 1500));

            connection.Execute("INSERT INTO group_memberships (user_id, group_id) VALUES (7, 3)");
            store.Save(new GroupInvitation(1, 7, 1, 5, DateTimeOffset.UtcNow));
            Assert.Equal(GroupJoinOutcome.LimitReached, store.Accept(7, 1, 1));
            Assert.NotNull(store.Find(1, 7));

            store.Save(new GroupInvitation(3, 7, 1, 5, DateTimeOffset.UtcNow));
            Assert.Equal(GroupJoinOutcome.AlreadyPresent, store.Accept(7, 3, 1500));
            Assert.Null(store.Find(3, 7));

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_memberships"));
            await Task.CompletedTask;
        });
    }

    [GroupParticipationDatabaseFact]
    public async Task DeletingTheGroupOrTheUserDeletesTheirInvitations()
    {
        await WithSchema(async (connection, store) =>
        {
            store.Save(new GroupInvitation(1, 7, 1, 5, DateTimeOffset.UtcNow));
            store.Save(new GroupInvitation(2, 8, 1, 5, DateTimeOffset.UtcNow));

            connection.Execute("DELETE FROM `groups` WHERE id = 1");
            connection.Execute("DELETE FROM user_statistics WHERE id = 8");
            connection.Execute("DELETE FROM users WHERE id = 8");

            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM group_invitations"));
            await Task.CompletedTask;
        });
    }

    private static async Task WithSchema(Func<MySqlConnection, GroupInvitationStore, Task> body)
    {
        var server = Environment.GetEnvironmentVariable("PLUS_GROUP_PARTICIPATION_TEST_CONNECTION_STRING")!;
        var schema = "task_group_invitation_tests_" + Guid.NewGuid().ToString("N")[..12];
        var options = new MySqlConnectionStringBuilder(server) { Database = schema, AllowZeroDateTime = true, ConvertZeroDateTime = true };

        using (var admin = new MySqlConnection(server)) {
            admin.Open();
            admin.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }

        try {
            using var connection = new MySqlConnection(options.ConnectionString);
            connection.Open();
            var fresh = File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql"));
            connection.Execute("SET FOREIGN_KEY_CHECKS = 0");

            foreach (var table in new[] { "users", "groups", "group_memberships", "group_requests", "user_statistics" }) {
                connection.Execute(ExtractTable(fresh, table));
            }

            connection.Execute("SET FOREIGN_KEY_CHECKS = 1");
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(Migration));
            connection.Execute(migration);
            connection.Execute(migration);
            connection.Execute("INSERT INTO users (id, username, auth_ticket) VALUES (1, 'owner', ''), (7, 'member', ''), (8, 'other', '')");
            connection.Execute("INSERT INTO user_statistics (id) VALUES (1), (7), (8)");
            connection.Execute("INSERT INTO `groups` (id, name, `desc`, badge, owner_id) VALUES (1, 'One', '', 'b', 1), (2, 'Two', '', 'b', 1), (3, 'Three', '', 'b', 1)");
            await body(connection, new GroupInvitationStore(new HabbiconDatabaseTests.TestDatabase(options.ConnectionString)));
        }
        finally {
            using var admin = new MySqlConnection(server);
            admin.Open();
            admin.Execute($"DROP DATABASE IF EXISTS `{schema}`");
        }
    }

    private static string ExtractTable(string dump, string table)
    {
        var start = dump.IndexOf($"CREATE TABLE `{table}` (", StringComparison.Ordinal);

        if (start < 0) {
            throw new InvalidOperationException($"The fresh install has no table {table}.");
        }

        var end = dump.IndexOf(';', dump.IndexOf("ENGINE=", start, StringComparison.Ordinal));

        return dump[start..(end + 1)];
    }
}
