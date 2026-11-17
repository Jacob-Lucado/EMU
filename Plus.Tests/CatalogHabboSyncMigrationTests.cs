using Dapper;
using MySqlConnector;
using Xunit;

namespace Plus.Tests;

// Runs Database/Migrations/140_CatalogHabboSync.sql on a small catalog in the current schema.
public sealed class CatalogHabboSyncMigrationTests
{
    private const string MigrationFile = "Database/Migrations/140_CatalogHabboSync.sql";

    [Fact]
    public void TheMigrationFileExistsAndIsEmbeddedByNumber()
    {
        var sql = File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile));

        Assert.Contains("START TRANSACTION;", sql);
        Assert.EndsWith("COMMIT;", sql.TrimEnd());
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DELETE FROM catalog_", sql);
    }

    [RoomComponentDatabaseFact]
    public void ItAddsPagesAndOffersInHabbosOrderAndCanRunTwice()
    {
        InSchema(connection =>
        {
            Seed(connection);
            var migration = File.ReadAllText(HabbiconPacketTests.Repo(MigrationFile));

            connection.Execute(migration);
            var afterFirst = Snapshot(connection);
            connection.Execute(migration);
            Assert.Equal(afterFirst, Snapshot(connection));

            // New pages go where Habbo lists them: first under Hot now, then the old pages follow.
            Assert.Equal(["nov26gamefurni", "nov26clothcrackb", "nov26furni", "nov26craft", "hween26sc", "hween26furni"],
                connection.Query<string>("SELECT c.link FROM catalog_pages c JOIN catalog_pages p ON p.id = c.parent_id AND p.link = '2020_hotnow' WHERE c.catalog_mode = 'NORMAL' ORDER BY c.position, c.id"));
            // A page Habbo hides sits under Staff and needs the administrator permission.
            Assert.Equal(("staff", "catalog.pages.administrator", 1), connection.QuerySingle<(string, string, int)>(
                "SELECT p.link, c.required_permission, c.enabled FROM catalog_pages c JOIN catalog_pages p ON p.id = c.parent_id WHERE c.link = 'nov26crackb'"));
            // Habbo's images and texts come with the page. A page whose parent is missing is skipped; one whose previous sibling is missing goes last.
            Assert.Equal(["", "spromo_GameItems_Nov26", ""], connection.Query<string>("SELECT i.image FROM catalog_page_images i JOIN catalog_pages c ON c.id = i.page_id WHERE c.link = 'nov26gamefurni' ORDER BY i.slot"));
            Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_pages WHERE link = 'ecotron_transform'"));
            Assert.Equal(0, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_pages WHERE link = 'anc_jungle_bundle'"));

            // An offer gets Habbo's id, key and prices, and sells the furniture found by item name.
            Assert.Equal((3, 10, 5, 1), connection.QuerySingle<(int, int, int, int)>("SELECT cost_credits, cost_points, points_type, bulk_purchase FROM catalog_offers WHERE id = 35057"));
            Assert.Equal(["survival_c26_medpack"], connection.Query<string>(
                "SELECT f.item_name FROM catalog_offer_products op JOIN furniture f ON f.id = op.furniture_id WHERE op.offer_id = 35057"));
            // An offer with a missing furniture row is dropped as a whole.
            Assert.Equal(0, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_offers WHERE id = 35056"));
            Assert.Equal(0, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_page_offers WHERE offer_id = 35056"));
            // Existing offers and pages keep their rows.
            Assert.Equal(("Old offer", 7), connection.QuerySingle<(string, int)>("SELECT localization_key, cost_credits FROM catalog_offers WHERE id = 1000"));
            Assert.Equal(["ducket_c26_acorndecor", "old_ducket"], connection.Query<string>(
                "SELECT o.localization_key FROM catalog_page_offers po JOIN catalog_offers o ON o.id = po.offer_id JOIN catalog_pages p ON p.id = po.page_id WHERE p.link = 'ducket_furni' ORDER BY po.position"));
            // A Habbo offer id that this hotel already has is linked with its key checked.
            Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_page_offers po JOIN catalog_pages p ON p.id = po.page_id WHERE p.link = 'pet_acc_ducket' AND po.offer_id = 12283"));
            // The badge a bundle needs is defined.
            Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM badge_definitions WHERE code = 'WSH10'"));

            // Furniture with no public offer sells at price 0 on its line page under Staff > Other.
            Assert.Equal(("Xmas2026", 0), connection.QuerySingle<(string, int)>(
                "SELECT c.caption, o.cost_credits FROM catalog_page_offers po JOIN catalog_pages c ON c.id = po.page_id JOIN catalog_offers o ON o.id = po.offer_id WHERE o.localization_key = 'xmas_c26_giftduck'"));
            Assert.Equal(0, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_offers WHERE localization_key = 'xmas_c26_cupsnowman'"));

            // After migration 139 adds the missing furniture, a second run sells it.
            connection.Execute("INSERT INTO furniture (item_name, type) VALUES ('survival_c26_knife', 's'), ('xmas_c26_cupsnowman', 's')");
            connection.Execute(migration);
            Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_offers WHERE id = 35056"));
            Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM catalog_offers WHERE localization_key = 'xmas_c26_cupsnowman'"));
            var afterSecond = Snapshot(connection);
            connection.Execute(migration);
            Assert.Equal(afterSecond, Snapshot(connection));
        });
    }

    private static string Snapshot(MySqlConnection connection) => string.Join("\n", new[] { "catalog_pages", "catalog_offers", "catalog_offer_products", "catalog_page_offers", "catalog_page_images", "catalog_page_texts", "badge_definitions" }
        .Select(table => table + " " + connection.QuerySingle<long>($"SELECT COALESCE(SUM(CRC32(CONCAT_WS('|', {string.Join(", ", connection.Query<string>($"SELECT COLUMN_NAME FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' ORDER BY ordinal_position").Select(c => $"`{c}`"))}))), 0) FROM `{table}`")));

    private static void Seed(MySqlConnection connection) => connection.Execute("""
        CREATE TABLE acl_permissions (`key` VARCHAR(191) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY) ENGINE=InnoDB;
        INSERT INTO acl_permissions VALUES ('catalog.pages.administrator');
        CREATE TABLE badge_definitions (code VARCHAR(35) NOT NULL PRIMARY KEY, required_right VARCHAR(191) NOT NULL DEFAULT '') ENGINE=InnoDB;
        CREATE TABLE catalog_bot_presets (id INT NOT NULL PRIMARY KEY) ENGINE=InnoDB;
        CREATE TABLE habbicons (id INT NOT NULL PRIMARY KEY) ENGINE=InnoDB;
        CREATE TABLE furniture (id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY, item_name VARCHAR(70) NOT NULL, type ENUM('s','i','e','h','v','r','b','p') NOT NULL DEFAULT 's') ENGINE=InnoDB;
        CREATE TABLE catalog_pages (
            id INT NOT NULL AUTO_INCREMENT, parent_id INT DEFAULT NULL, caption VARCHAR(128) NOT NULL, icon INT NOT NULL DEFAULT 0,
            required_permission VARCHAR(191) CHARACTER SET ascii COLLATE ascii_bin DEFAULT NULL, position INT NOT NULL DEFAULT 0, link VARCHAR(128) DEFAULT NULL,
            layout VARCHAR(64) NOT NULL DEFAULT 'default_3x3', catalog_mode ENUM('NORMAL','BUILDERS_CLUB') NOT NULL DEFAULT 'NORMAL',
            visible TINYINT(1) NOT NULL DEFAULT 1, enabled TINYINT(1) NOT NULL DEFAULT 1, required_club_level INT NOT NULL DEFAULT 0,
            PRIMARY KEY (id), UNIQUE KEY mode_link (catalog_mode, link), KEY tree (parent_id, position, id),
            CONSTRAINT fk_catalog_pages_parent FOREIGN KEY (parent_id) REFERENCES catalog_pages (id),
            CONSTRAINT fk_catalog_pages_permission FOREIGN KEY (required_permission) REFERENCES acl_permissions (`key`)
        ) ENGINE=InnoDB AUTO_INCREMENT=2100000100;
        CREATE TABLE catalog_offers (
            id INT NOT NULL AUTO_INCREMENT, localization_key VARCHAR(100) NOT NULL, cost_credits INT UNSIGNED NOT NULL DEFAULT 0, cost_points INT UNSIGNED NOT NULL DEFAULT 0,
            points_type INT UNSIGNED NOT NULL DEFAULT 0, club_level TINYINT UNSIGNED NOT NULL DEFAULT 0, bulk_purchase TINYINT(1) NOT NULL DEFAULT 1,
            enabled TINYINT(1) NOT NULL DEFAULT 1, preview_image VARCHAR(255) NOT NULL DEFAULT '', PRIMARY KEY (id)
        ) ENGINE=InnoDB AUTO_INCREMENT=1900000400;
        CREATE TABLE catalog_offer_products (
            offer_id INT NOT NULL, position TINYINT UNSIGNED NOT NULL, product_type ENUM('furni','effect','badge','bot','pet','habbicon','builders_club','builders_club_addon') NOT NULL,
            furniture_id INT UNSIGNED DEFAULT NULL, effect_id INT DEFAULT NULL, badge_code VARCHAR(35) DEFAULT NULL, bot_preset_id INT DEFAULT NULL, pet_type INT DEFAULT NULL,
            habbicon_id INT DEFAULT NULL, amount INT UNSIGNED NOT NULL DEFAULT 1, extra_param VARCHAR(1024) NOT NULL DEFAULT '', PRIMARY KEY (offer_id, position),
            CONSTRAINT fk_p_badge FOREIGN KEY (badge_code) REFERENCES badge_definitions (code), CONSTRAINT fk_p_bot FOREIGN KEY (bot_preset_id) REFERENCES catalog_bot_presets (id),
            CONSTRAINT fk_p_furniture FOREIGN KEY (furniture_id) REFERENCES furniture (id), CONSTRAINT fk_p_habbicon FOREIGN KEY (habbicon_id) REFERENCES habbicons (id),
            CONSTRAINT fk_p_offer FOREIGN KEY (offer_id) REFERENCES catalog_offers (id) ON DELETE CASCADE ON UPDATE CASCADE,
            CONSTRAINT ck_p_amount CHECK (amount BETWEEN 1 AND 1000)
        ) ENGINE=InnoDB;
        CREATE TABLE catalog_page_offers (
            page_id INT NOT NULL, offer_id INT NOT NULL, position INT NOT NULL DEFAULT 0, PRIMARY KEY (page_id, offer_id), KEY offer_id (offer_id),
            CONSTRAINT fk_po_offer FOREIGN KEY (offer_id) REFERENCES catalog_offers (id) ON DELETE CASCADE ON UPDATE CASCADE,
            CONSTRAINT fk_po_page FOREIGN KEY (page_id) REFERENCES catalog_pages (id) ON DELETE CASCADE
        ) ENGINE=InnoDB;
        CREATE TABLE catalog_page_images (page_id INT NOT NULL, slot TINYINT UNSIGNED NOT NULL, image VARCHAR(255) NOT NULL, PRIMARY KEY (page_id, slot),
            CONSTRAINT fk_pi_page FOREIGN KEY (page_id) REFERENCES catalog_pages (id) ON DELETE CASCADE) ENGINE=InnoDB;
        CREATE TABLE catalog_page_texts (page_id INT NOT NULL, slot TINYINT UNSIGNED NOT NULL, text TEXT NOT NULL, PRIMARY KEY (page_id, slot),
            CONSTRAINT fk_pt_page FOREIGN KEY (page_id) REFERENCES catalog_pages (id) ON DELETE CASCADE) ENGINE=InnoDB;

        INSERT INTO catalog_pages (id, parent_id, caption, position, link, catalog_mode, required_permission, enabled) VALUES
            (1, NULL, 'Furni', 1, 'furni', 'NORMAL', NULL, 1),
            (2, 1, 'Hot now', 1, '2020_hotnow', 'NORMAL', NULL, 0),
            (3, 2, 'Old hot page', 0, 'hween26furni', 'NORMAL', NULL, 1),
            (4, 1, 'The Ducket Shop', 2, 'ducket_furni', 'NORMAL', NULL, 1),
            (5, 1, 'Pet food', 3, 'pet_acc_ducket', 'NORMAL', NULL, 1),
            (6, NULL, 'Staff', 6, 'staff', 'NORMAL', 'catalog.pages.administrator', 1),
            (7, 6, 'Other', 3, NULL, 'NORMAL', 'catalog.pages.administrator', 0),
            (8, 7, 'Xmas2025', 0, NULL, 'NORMAL', 'catalog.pages.administrator', 1);
        INSERT INTO furniture (id, item_name, type) VALUES (10, 'survival_c26_medpack', 's'), (11, 'old_ducket', 's'), (12, 'milkbowl', 's'), (13, 'xmas_c26_giftduck', 's'), (14, 'ducket_c26_acorndecor', 's');
        INSERT INTO catalog_offers (id, localization_key, cost_credits) VALUES (1000, 'Old offer', 7), (1001, 'old_ducket', 1), (12283, 'milkbowl', 0);
        INSERT INTO catalog_offer_products (offer_id, position, product_type, furniture_id) VALUES (1000, 0, 'furni', 11), (1001, 0, 'furni', 11), (12283, 0, 'furni', 12);
        INSERT INTO catalog_page_offers (page_id, offer_id, position) VALUES (4, 1001, 0);
        """);

    private static void InSchema(Action<MySqlConnection> run)
    {
        var options = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_COMPONENT_DATABASE")!) { AllowUserVariables = true };
        using var admin = new MySqlConnection(options.ConnectionString);
        admin.Open();
        var schema = "catalog_habbo_sync_" + Guid.NewGuid().ToString("N");
        admin.Execute($"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try {
            options.Database = schema;
            using var connection = new MySqlConnection(options.ConnectionString);
            connection.Open();
            run(connection);
        }
        finally {
            admin.Execute($"DROP DATABASE `{schema}`");
        }
    }
}
