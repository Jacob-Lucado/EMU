using Dapper;
using MySqlConnector;
using Plus.Database.Migrator;
using Xunit;

namespace Plus.Tests;

public sealed class LegacyProbeDatabaseTests : IDisposable
{
    private const string WiredFurniture = "79_WiredFurnitureAndCatalogue.sql";
    private const string WiredArchiveText = "83_WiredArchiveText.sql";
    private const string ArchiveText = "Archived Wired boxes that are still available for legacy reasons, but are not recommended to use in new Wired setups";

    private readonly MySqlConnectionStringBuilder _server;
    private readonly string _schema = "legacy_probe_tests_" + Guid.NewGuid().ToString("N")[..12];

    public LegacyProbeDatabaseTests()
    {
        _server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SchemaMigrationDatabaseFactAttribute.Variable) ?? "Server=localhost");

        if (Environment.GetEnvironmentVariable(SchemaMigrationDatabaseFactAttribute.Variable) == null) {
            return;
        }

        using var server = Connect(string.Empty);
        server.Execute($"CREATE DATABASE `{_schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        using var db = Connect(_schema);
        db.Execute("""
            CREATE TABLE furniture (id INT AUTO_INCREMENT PRIMARY KEY, item_name VARCHAR(70) NOT NULL, name VARCHAR(100) NOT NULL DEFAULT '',
                type CHAR(1) NOT NULL DEFAULT 's', sprite_id INT NOT NULL DEFAULT 0, interaction_type VARCHAR(50) NOT NULL DEFAULT 'default');
            CREATE TABLE achievements (id INT AUTO_INCREMENT PRIMARY KEY, official_id INT NULL);
            CREATE TABLE catalog_pages (id INT PRIMARY KEY, link VARCHAR(128) NOT NULL);
            CREATE TABLE catalog_page_texts (page_id INT NOT NULL, slot INT NOT NULL, text TEXT NOT NULL, PRIMARY KEY (page_id, slot));
            """);
    }

    public void Dispose()
    {
        if (Environment.GetEnvironmentVariable(SchemaMigrationDatabaseFactAttribute.Variable) != null) {
            using var server = Connect(string.Empty);
            server.Execute($"DROP DATABASE IF EXISTS `{_schema}`");
        }
    }

    [SchemaMigrationDatabaseFact]
    public void WiredFurnitureIsAppliedOnAHotelWithoutItsFurniture()
    {
        Assert.True(Applied(WiredFurniture));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredFurnitureIsNotAppliedWhileTheClickTileHasItsOldInteraction()
    {
        Execute("INSERT INTO furniture (item_name, sprite_id, interaction_type) VALUES ('room_invisible_click_tile', 14561, 'default')");

        Assert.False(Applied(WiredFurniture));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredFurnitureIsNotAppliedWhileABoxHasItsPlaceholderName()
    {
        Execute("INSERT INTO furniture (item_name, name, sprite_id) VALUES ('wf_cnd_not_wearing_b', 'wf_cnd_not_wearing_b name', 5446)");

        Assert.False(Applied(WiredFurniture));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredFurnitureIsNotAppliedWithoutItsFurnitureOnAHotelFromBeforeOfficialAchievements()
    {
        Execute("ALTER TABLE achievements DROP COLUMN official_id");

        Assert.False(Applied(WiredFurniture));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredArchiveTextIsAppliedOnACatalogueWithoutTheArchivePage()
    {
        Execute("INSERT INTO furniture (item_name, interaction_type) VALUES ('conf_chooser_disable', 'chooser_disable')");

        Assert.True(Applied(WiredArchiveText));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredArchiveTextIsAppliedWhenAnAdminChangedTheText()
    {
        Execute("INSERT INTO furniture (item_name, interaction_type) VALUES ('conf_chooser_disable', 'chooser_disable')");
        Execute("INSERT INTO catalog_pages VALUES (5, 'wired_archive'); INSERT INTO catalog_page_texts VALUES (5, 0, 'Old boxes')");

        Assert.True(Applied(WiredArchiveText));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredArchiveTextIsAppliedWhenThePageHasTheHabboText()
    {
        Execute($"INSERT INTO catalog_pages VALUES (5, 'wired_archive'); INSERT INTO catalog_page_texts VALUES (5, 0, '{ArchiveText}')");

        Assert.True(Applied(WiredArchiveText));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredArchiveTextIsNotAppliedWhileTheArchivePageTextIsEmpty()
    {
        Execute("INSERT INTO furniture (item_name, interaction_type) VALUES ('conf_chooser_disable', 'chooser_disable')");
        Execute("INSERT INTO catalog_pages VALUES (5, 'wired_archive'), (6, 'plus_wired_more'); INSERT INTO catalog_page_texts VALUES (5, 0, '')");

        Assert.False(Applied(WiredArchiveText));
    }

    [SchemaMigrationDatabaseFact]
    public void WiredArchiveTextIsNotAppliedBeforeWiredCatalogueParity()
    {
        Assert.False(Applied(WiredArchiveText));
    }

    private bool Applied(string migration)
    {
        using var db = Connect(_schema);
        var script = MigrationScripts.Load(typeof(SchemaMigrator).Assembly).Single(s => s.Id == migration);

        return script.Probe!.Evaluate(new SchemaSnapshot(db));
    }

    private void Execute(string sql)
    {
        using var db = Connect(_schema);
        db.Execute(sql);
    }

    private MySqlConnection Connect(string database)
    {
        var connection = new MySqlConnection(new MySqlConnectionStringBuilder(_server.ConnectionString) { Database = database, AllowUserVariables = true }.ConnectionString);
        connection.Open();

        return connection;
    }
}
