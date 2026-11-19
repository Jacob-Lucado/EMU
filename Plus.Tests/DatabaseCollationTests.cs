using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Plus.Database;
using Plus.Database.Migrator;
using Xunit;

namespace Plus.Tests;

public sealed class DatabaseCollationTests
{
    private const string MigrationFile = "Database/Migrations/162_DatabaseCollation.sql";

    // A hotel owner may create the database without naming a collation, so it gets the server default
    // (utf8mb4_general_ci on many MariaDB 10 servers, utf8mb4_uca1400_ai_ci on MariaDB 11.5 and newer).
    [SchemaMigrationDatabaseFact]
    public async Task ANewHotelDatabaseEndsWithOneCollationWhateverItsDefault()
    {
        foreach (var options in new[] { "", "CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci" }) {
            await InDatabase(options, async (server, schema, connection) =>
            {
                var migrator = new SchemaMigrator(Configuration(server, schema), NullLogger<SchemaMigrator>.Instance, MigrationScripts.Load(),
                    () => File.ReadAllText(HabbiconPacketTests.Repo("Database/FreshInstall.sql")));

                Assert.True(await migrator.Run(MigrationMode.Apply), $"database created with '{options}'");

                Assert.Equal("utf8mb4_unicode_ci", DatabaseCollation(connection));
                Assert.Empty(OtherCollations(connection));
            });
        }
    }

    [SchemaMigrationDatabaseFact]
    public async Task MigrationMovesADriftedDatabaseToOneCollationAndKeepsItsDataAndKeys()
    {
        await InDatabase("CHARACTER SET latin1", (_, _, connection) =>
        {
            CreateDriftedTables(connection);

            Apply(connection);

            Assert.Equal("utf8mb4_unicode_ci", DatabaseCollation(connection));
            Assert.Empty(OtherCollations(connection));
            Assert.Equal(new[]
            {
                "child.parent_code utf8mb4_unicode_ci", "notes.body utf8mb4_unicode_ci", "notes.hash utf8mb4_bin", "notes.kind utf8mb4_unicode_ci",
                "notes.payload utf8mb4_unicode_ci", "notes.token ascii_bin", "notes.upper_body utf8mb4_unicode_ci", "pair_child.code utf8mb4_unicode_ci",
                "pair_child.kind utf8mb4_unicode_ci", "pair_parent.code utf8mb4_unicode_ci", "pair_parent.kind utf8mb4_unicode_ci",
                "parent.code utf8mb4_unicode_ci", "parent.label utf8mb4_unicode_ci",
            }, connection.Query<string>("SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', COLLATION_NAME) FROM information_schema.COLUMNS " +
                                        "WHERE TABLE_SCHEMA = DATABASE() AND COLLATION_NAME IS NOT NULL ORDER BY 1"));

            Assert.Equal(("café", "CAFÉ", "b"), connection.QuerySingle<(string, string, string)>("SELECT body, upper_body, kind FROM notes WHERE id = 1"));
            Assert.Equal(new[] { "kind enum('a','b') NO 'a'", "label varchar(50) NO 'none'", "upper_body varchar(50) YES VIRTUAL GENERATED" },
                connection.Query<string>("SELECT CONCAT_WS(' ', COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, NULLIF(COLUMN_DEFAULT, 'NULL'), NULLIF(EXTRA, '')) " +
                                         "FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND COLUMN_NAME IN ('kind', 'label', 'upper_body') " +
                                         "AND TABLE_NAME IN ('notes', 'parent') ORDER BY COLUMN_NAME"));
            Assert.Equal("🎉 party", connection.QuerySingle<string>("SELECT label FROM parent WHERE code = 'p1'"));
            Assert.Equal("none", connection.QuerySingle<string>("SELECT label FROM parent WHERE code = 'p2'"));
            Assert.Equal("shown in the catalog", connection.QuerySingle<string>(
                "SELECT COLUMN_COMMENT FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'parent' AND COLUMN_NAME = 'label'"));
            Assert.Equal("PARENT_CODE", connection.QuerySingle<string>("SELECT GROUP_CONCAT(COLUMN_NAME) FROM information_schema.KEY_COLUMN_USAGE " +
                "WHERE TABLE_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'fk_child_parent'").ToUpperInvariant());

            Assert.ThrowsAny<MySqlException>(() => connection.Execute("INSERT INTO notes (id, body, payload) VALUES (2, 'x', 'not json')"));
            Assert.ThrowsAny<MySqlException>(() => connection.Execute("INSERT INTO child VALUES (2, 'missing')"));
            connection.Execute("DELETE FROM parent WHERE code = 'p1'");
            Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM child"));
            connection.Execute("UPDATE pair_parent SET code = 'c2' WHERE kind = 'k'");
            Assert.Equal("c2", connection.QuerySingle<string>("SELECT code FROM pair_child WHERE id = 1"));
        });
    }

    [SchemaMigrationDatabaseFact]
    public async Task AUniqueCollisionLeavesDataAndForeignKeysInPlace()
    {
        await InDatabase("CHARACTER SET latin1", (_, _, connection) =>
        {
            CreateDriftedTables(connection);
            connection.Execute("CREATE TABLE names (site INT, name VARCHAR(20), UNIQUE (site, name(2))) DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci; " +
                               "INSERT INTO names VALUES (1, 'ß'), (1, 'ssuffix'), (NULL, 'a'), (NULL, 'a')");

            Assert.ThrowsAny<MySqlException>(() => Apply(connection));
            Assert.Equal(4, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM names"));
            Assert.Equal("latin1_swedish_ci", DatabaseCollation(connection));
            Assert.ThrowsAny<MySqlException>(() => connection.Execute("INSERT INTO child VALUES (2, 'missing')"));

            connection.Execute("DELETE FROM names WHERE name = 'ssuffix'");
            Apply(connection);
            Assert.Empty(OtherCollations(connection));
        });
    }

    [SchemaMigrationDatabaseFact]
    public async Task RestoringAKeyValidatesExistingChildRowsAndCanBeRetried()
    {
        await InDatabase("CHARACTER SET latin1", (_, _, connection) =>
        {
            CreateDriftedTables(connection);
            connection.Execute("INSERT INTO parent VALUES ('s', 'parent'); INSERT INTO child VALUES (3, 'ß')");

            Assert.ThrowsAny<MySqlException>(() => Apply(connection));
            connection.Execute("UPDATE child SET parent_code = 's' WHERE id = 3");
            Apply(connection);

            Assert.Equal(new[] { "fk_child_parent", "fk_pair" }, connection.Query<string>(
                "SELECT CONSTRAINT_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() ORDER BY 1"));
            Assert.ThrowsAny<MySqlException>(() => connection.Execute("INSERT INTO child VALUES (2, 'missing')"));
        });
    }

    [SchemaMigrationDatabaseFact]
    public async Task MigrationPreservesLiteralsWhenBackslashEscapesAreDisabled()
    {
        await InDatabase("CHARACTER SET latin1", (_, _, connection) =>
        {
            connection.Execute("""SET SESSION sql_mode = 'NO_BACKSLASH_ESCAPES'; CREATE TABLE paths (path VARCHAR(50) DEFAULT 'a\b' COMMENT 'c\d')""");

            ApplyInSession(connection);

            connection.Execute("INSERT INTO paths () VALUES ()");
            Assert.Equal(@"a\b", connection.QuerySingle<string>("SELECT path FROM paths"));
            Assert.Equal(@"c\d", connection.QuerySingle<string>(
                "SELECT COLUMN_COMMENT FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'paths' AND COLUMN_NAME = 'path'"));
        });
    }

    [SchemaMigrationDatabaseFact]
    public async Task RunningTheMigrationAgainChangesNothing()
    {
        await InDatabase("CHARACTER SET latin1", async (_, _, connection) =>
        {
            CreateDriftedTables(connection);
            Apply(connection);
            var before = Tables(connection);
            // CREATE_TIME has a resolution of one second; a rebuilt table must show a later time.
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            Apply(connection);

            Assert.Equal(before, Tables(connection));
        });
    }

    private static void CreateDriftedTables(MySqlConnection connection) => connection.Execute("""
        CREATE TABLE parent (
            code VARCHAR(20) NOT NULL PRIMARY KEY,
            label VARCHAR(50) NOT NULL DEFAULT 'none' COMMENT 'shown in the catalog'
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
        CREATE TABLE child (
            id INT NOT NULL PRIMARY KEY,
            parent_code VARCHAR(20) NULL,
            CONSTRAINT fk_child_parent FOREIGN KEY (parent_code) REFERENCES parent (code) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
        CREATE TABLE notes (
            id INT NOT NULL PRIMARY KEY,
            body VARCHAR(50) NOT NULL,
            payload LONGTEXT NULL CHECK (JSON_VALID(payload)),
            token VARCHAR(10) CHARACTER SET ascii COLLATE ascii_bin NULL,
            hash VARCHAR(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL,
            upper_body VARCHAR(50) AS (UPPER(body)) VIRTUAL,
            kind ENUM('a', 'b') NOT NULL DEFAULT 'a'
        ) ENGINE=InnoDB;
        CREATE TABLE pair_parent (
            kind VARCHAR(10) NOT NULL,
            code VARCHAR(10) NOT NULL,
            PRIMARY KEY (kind, code)
        ) ENGINE=InnoDB;
        CREATE TABLE pair_child (
            id INT NOT NULL PRIMARY KEY,
            kind VARCHAR(10) NOT NULL,
            code VARCHAR(10) NOT NULL,
            CONSTRAINT fk_pair FOREIGN KEY (kind, code) REFERENCES pair_parent (kind, code) ON UPDATE CASCADE
        ) ENGINE=InnoDB;
        INSERT INTO parent VALUES ('p1', '🎉 party');
        INSERT INTO parent (code) VALUES ('p2');
        INSERT INTO child VALUES (1, 'p1');
        INSERT INTO notes (id, body, payload, token, hash, kind) VALUES (1, 'café', '{"a": 1}', 'T1', 'h1', 'b');
        INSERT INTO pair_parent VALUES ('k', 'c1');
        INSERT INTO pair_child VALUES (1, 'k', 'c1');
        """);

    // Its own session, like the migrator.
    private static void Apply(MySqlConnection connection)
    {
        using var session = new MySqlConnection(connection.ConnectionString);
        session.Open();
        ApplyInSession(session);
    }

    private static void ApplyInSession(MySqlConnection session)
    {
        foreach (var statement in SqlScript.Split(File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile)))) {
            session.Execute(statement);
        }
    }

    private static string DatabaseCollation(MySqlConnection connection) =>
        connection.QuerySingle<string>("SELECT DEFAULT_COLLATION_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = DATABASE()");

    private static List<string> OtherCollations(MySqlConnection connection) => connection.Query<string>("""
        SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', COLLATION_NAME) FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin', 'ascii_bin', 'ascii_general_ci')
        UNION ALL
        SELECT CONCAT(TABLE_NAME, ' ', TABLE_COLLATION) FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' AND TABLE_COLLATION <> 'utf8mb4_unicode_ci'
        """).ToList();

    private static List<string> Tables(MySqlConnection connection) => connection
        .Query<(string Name, DateTime Created)>("SELECT TABLE_NAME, CREATE_TIME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME")
        .Select(t => $"{t.Created:O} {connection.QuerySingle<(string, string)>($"SHOW CREATE TABLE `{t.Name}`").Item2}")
        .ToList();

    private static async Task InDatabase(string options, Func<MySqlConnectionStringBuilder, string, MySqlConnection, Task> body)
    {
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SchemaMigrationDatabaseFactAttribute.Variable)) { Database = string.Empty };
        var schema = "collation_tests_" + Guid.NewGuid().ToString("N")[..12];
        using var root = new MySqlConnection(server.ConnectionString);
        root.Execute($"CREATE DATABASE `{schema}` {options}");

        try {
            using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(server.ConnectionString) { Database = schema, AllowUserVariables = true, PersistSecurityInfo = true }.ConnectionString);
            connection.Open();
            await body(server, schema, connection);
        }
        finally {
            root.Execute($"DROP DATABASE `{schema}`");
        }
    }

    private static Task InDatabase(string options, Action<MySqlConnectionStringBuilder, string, MySqlConnection> body) =>
        InDatabase(options, (server, schema, connection) =>
        {
            body(server, schema, connection);

            return Task.CompletedTask;
        });

    private static DatabaseConfiguration Configuration(MySqlConnectionStringBuilder server, string schema) => new()
    {
        Hostname = server.Server,
        Port = server.Port,
        Username = server.UserID,
        Password = server.Password,
        Name = schema,
        Migrations = new MigrationConfiguration { Backup = false },
    };
}
