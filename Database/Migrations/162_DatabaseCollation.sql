-- 162_DatabaseCollation
-- One collation for the whole database, as migration 70 set up: the database default, every table and every text
-- column use utf8mb4_unicode_ci. Columns that are deliberately binary (utf8mb4_bin) or ASCII keep their collation.
-- A table created without naming a collation takes the server or database default (utf8mb4_uca1400_ai_ci on
-- MariaDB 11.5 and newer), and string joins and foreign keys between two collations fail.
-- Only tables and columns that differ are changed, so a database that already has one collation is left alone.

-- Metadata literals use backslash escapes. Strict conversion rejects values that no longer fit.
SET SESSION sql_mode = CONCAT_WS(',', NULLIF(TRIM(BOTH ',' FROM
    REPLACE(CONCAT(',', @@sql_mode, ','), ',NO_BACKSLASH_ESCAPES,', ',')), ''), 'STRICT_ALL_TABLES');
SET SESSION group_concat_max_len = 1048576;

CREATE TEMPORARY TABLE `migration_162_columns` AS
    SELECT c.TABLE_NAME, c.COLUMN_NAME, c.ORDINAL_POSITION,
           IF(RIGHT(c.COLLATION_NAME, 4) = '_bin', 'utf8mb4_bin', 'utf8mb4_unicode_ci') AS target_collation,
           CONCAT('MODIFY `', REPLACE(c.COLUMN_NAME, '`', '``'), '` ', c.COLUMN_TYPE, ' CHARACTER SET utf8mb4 COLLATE ',
                  IF(RIGHT(c.COLLATION_NAME, 4) = '_bin', 'utf8mb4_bin', 'utf8mb4_unicode_ci'),
                  IF(c.EXTRA LIKE '%GENERATED%',
                     CONCAT(' GENERATED ALWAYS AS (', c.GENERATION_EXPRESSION, ') ', IF(c.EXTRA LIKE 'STORED%', 'STORED', 'VIRTUAL')),
                     CONCAT(IF(c.IS_NULLABLE = 'YES', ' NULL', ' NOT NULL'),
                            IF(c.COLUMN_DEFAULT IS NULL, '', CONCAT(' DEFAULT ',
                               IF(c.COLUMN_DEFAULT = 'NULL' OR c.COLUMN_DEFAULT LIKE '''%', c.COLUMN_DEFAULT, CONCAT('(', c.COLUMN_DEFAULT, ')')))))),
                  IF(c.EXTRA LIKE '%INVISIBLE%', ' INVISIBLE', ''),
                  IF(c.COLUMN_COMMENT = '', '', CONCAT(' COMMENT ', QUOTE(c.COLUMN_COMMENT))),
                  COALESCE((SELECT CONCAT(' CHECK (', k.CHECK_CLAUSE, ')') FROM information_schema.CHECK_CONSTRAINTS k
                            WHERE k.CONSTRAINT_SCHEMA = c.TABLE_SCHEMA AND k.TABLE_NAME = c.TABLE_NAME
                              AND k.CONSTRAINT_NAME = c.COLUMN_NAME AND k.LEVEL = 'Column'), '')) AS `definition`
    FROM information_schema.COLUMNS c
    JOIN information_schema.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_TYPE = 'BASE TABLE'
    WHERE c.TABLE_SCHEMA = DATABASE() AND c.CHARACTER_SET_NAME IS NOT NULL AND c.CHARACTER_SET_NAME <> 'ascii'
      AND c.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin');

DELIMITER //
BEGIN NOT ATOMIC
    DECLARE done BOOL DEFAULT FALSE;
    DECLARE source_table, key_name VARCHAR(64);
    DECLARE parts, non_null LONGTEXT;
    DECLARE problem VARCHAR(512);
    DECLARE indexes_to_check CURSOR FOR
        SELECT s.TABLE_NAME, s.INDEX_NAME,
               GROUP_CONCAT(IF(d.target_collation IS NULL, s.part,
                               CONCAT('CONVERT(', s.part, ' USING utf8mb4) COLLATE ', d.target_collation)) ORDER BY s.SEQ_IN_INDEX),
               GROUP_CONCAT(CONCAT(s.identifier, ' IS NOT NULL') ORDER BY s.SEQ_IN_INDEX SEPARATOR ' AND ')
        FROM (
            SELECT k.*, IF(k.SUB_PART IS NULL, k.identifier, CONCAT('LEFT(', k.identifier, ',', k.SUB_PART, ')')) AS part
            FROM (
                SELECT TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX, COLUMN_NAME, SUB_PART,
                       CONCAT('`', REPLACE(COLUMN_NAME, '`', '``'), '`') AS identifier
                FROM information_schema.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE() AND NON_UNIQUE = 0
            ) k
        ) s
        LEFT JOIN migration_162_columns d ON d.TABLE_NAME = s.TABLE_NAME AND d.COLUMN_NAME = s.COLUMN_NAME
        GROUP BY s.TABLE_NAME, s.INDEX_NAME
        HAVING COUNT(d.COLUMN_NAME) > 0;
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    -- Compare complete unique keys before dropping constraints. A unique key permits repeated NULLs.
    OPEN indexes_to_check;
    check_keys: LOOP
        FETCH indexes_to_check INTO source_table, key_name, parts, non_null;
        IF done THEN
            LEAVE check_keys;
        END IF;
        EXECUTE IMMEDIATE CONCAT('SELECT EXISTS(SELECT 1 FROM `', REPLACE(source_table, '`', '``'),
                                 '` WHERE ', non_null, ' GROUP BY ', parts, ' HAVING COUNT(*) > 1) INTO @migration_162_collision');
        IF @migration_162_collision THEN
            SET problem = CONCAT('Collation change would duplicate a key in ', source_table, '.', key_name);
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = problem;
        END IF;
    END LOOP;
    CLOSE indexes_to_check;
END//

IF EXISTS (SELECT 1 FROM migration_162_columns)
   OR EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
              AND (TABLE_COLLATION <> 'utf8mb4_unicode_ci' OR TABLE_NAME = 'migration_162_keys'))
   OR EXISTS (SELECT 1 FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = DATABASE() AND DEFAULT_COLLATION_NAME <> 'utf8mb4_unicode_ci')
THEN
    IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'migration_162_keys'
               AND TABLE_COMMENT <> 'PlusEMU migration 162 foreign keys') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Table migration_162_keys already exists and is not the migration recovery table';
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.KEY_COLUMN_USAGE k JOIN migration_162_columns c
               ON (k.TABLE_SCHEMA = DATABASE() AND k.TABLE_NAME = c.TABLE_NAME AND k.COLUMN_NAME = c.COLUMN_NAME)
               OR (k.REFERENCED_TABLE_SCHEMA = DATABASE() AND k.REFERENCED_TABLE_NAME = c.TABLE_NAME AND k.REFERENCED_COLUMN_NAME = c.COLUMN_NAME)
               WHERE k.TABLE_SCHEMA <> k.REFERENCED_TABLE_SCHEMA) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Migrate the cross-database foreign key and both columns together';
    END IF;

    ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

    -- MariaDB will not change the collation of a column in a foreign key, so those keys come off and go back unchanged.
    -- They stay in a real table until they are back, so a run that stops halfway restores them the next time.
    CREATE TABLE IF NOT EXISTS `migration_162_keys` (
        `table_name` VARCHAR(64) NOT NULL,
        `constraint_name` VARCHAR(64) NOT NULL,
        `addition` TEXT NOT NULL,
        PRIMARY KEY (`table_name`, `constraint_name`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='PlusEMU migration 162 foreign keys';

    INSERT IGNORE INTO `migration_162_keys` (`table_name`, `constraint_name`, `addition`)
        SELECT r.TABLE_NAME, r.CONSTRAINT_NAME,
               CONCAT('ADD CONSTRAINT `', REPLACE(r.CONSTRAINT_NAME, '`', '``'), '` FOREIGN KEY (',
                      GROUP_CONCAT(CONCAT('`', REPLACE(k.COLUMN_NAME, '`', '``'), '`') ORDER BY k.ORDINAL_POSITION), ') REFERENCES `', REPLACE(r.REFERENCED_TABLE_NAME, '`', '``'), '` (',
                      GROUP_CONCAT(CONCAT('`', REPLACE(k.REFERENCED_COLUMN_NAME, '`', '``'), '`') ORDER BY k.ORDINAL_POSITION), ') ON DELETE ', r.DELETE_RULE,
                      ' ON UPDATE ', r.UPDATE_RULE)
        FROM information_schema.REFERENTIAL_CONSTRAINTS r
        JOIN information_schema.KEY_COLUMN_USAGE k
          ON k.CONSTRAINT_SCHEMA = r.CONSTRAINT_SCHEMA AND k.TABLE_NAME = r.TABLE_NAME AND k.CONSTRAINT_NAME = r.CONSTRAINT_NAME
        LEFT JOIN information_schema.COLUMNS c
          ON c.TABLE_SCHEMA = k.TABLE_SCHEMA AND c.TABLE_NAME = k.TABLE_NAME AND c.COLUMN_NAME = k.COLUMN_NAME
        LEFT JOIN information_schema.COLUMNS p
          ON p.TABLE_SCHEMA = k.REFERENCED_TABLE_SCHEMA AND p.TABLE_NAME = k.REFERENCED_TABLE_NAME AND p.COLUMN_NAME = k.REFERENCED_COLUMN_NAME
        WHERE r.CONSTRAINT_SCHEMA = DATABASE()
        GROUP BY r.TABLE_NAME, r.CONSTRAINT_NAME, r.REFERENCED_TABLE_NAME, r.DELETE_RULE, r.UPDATE_RULE
        HAVING SUM(c.CHARACTER_SET_NAME IS NOT NULL AND c.CHARACTER_SET_NAME <> 'ascii' AND c.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin')
                OR p.CHARACTER_SET_NAME IS NOT NULL AND p.CHARACTER_SET_NAME <> 'ascii' AND p.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin')) > 0;

    CREATE TEMPORARY TABLE `migration_162_statements` (
        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        `statement` LONGTEXT NOT NULL
    );

    INSERT INTO `migration_162_statements` (`statement`)
        SELECT CONCAT('ALTER TABLE `', REPLACE(k.`table_name`, '`', '``'), '` ', GROUP_CONCAT(CONCAT('DROP FOREIGN KEY `', REPLACE(k.`constraint_name`, '`', '``'), '`') SEPARATOR ', '))
        FROM `migration_162_keys` k
        WHERE EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r
                      WHERE r.CONSTRAINT_SCHEMA = DATABASE() AND r.TABLE_NAME = k.`table_name` AND r.CONSTRAINT_NAME = k.`constraint_name`)
        GROUP BY k.`table_name`;

    INSERT INTO `migration_162_statements` (`statement`)
        SELECT CONCAT('ALTER TABLE `', REPLACE(TABLE_NAME, '`', '``'), '` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci, ',
                      GROUP_CONCAT(`definition` ORDER BY ORDINAL_POSITION SEPARATOR ', '))
        FROM `migration_162_columns` GROUP BY TABLE_NAME;

    INSERT INTO `migration_162_statements` (`statement`)
        SELECT CONCAT('ALTER TABLE `', REPLACE(t.TABLE_NAME, '`', '``'), '` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci')
        FROM information_schema.TABLES t
        WHERE t.TABLE_SCHEMA = DATABASE() AND t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_COLLATION <> 'utf8mb4_unicode_ci'
          AND NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS c
                          WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME AND c.CHARACTER_SET_NAME IS NOT NULL
                            AND c.CHARACTER_SET_NAME <> 'ascii' AND c.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin'));

    INSERT INTO `migration_162_statements` (`statement`)
        SELECT CONCAT('ALTER TABLE `', REPLACE(`table_name`, '`', '``'), '` ', GROUP_CONCAT(`addition` SEPARATOR ', '))
        FROM `migration_162_keys` GROUP BY `table_name`;

    SET FOREIGN_KEY_CHECKS = 1;

    BEGIN
        DECLARE finished BOOL DEFAULT FALSE;
        DECLARE next_statement LONGTEXT;
        DECLARE statements CURSOR FOR SELECT `statement` FROM `migration_162_statements` ORDER BY `id`;
        DECLARE CONTINUE HANDLER FOR NOT FOUND SET finished = TRUE;

        OPEN statements;
        run: LOOP
            FETCH statements INTO next_statement;
            IF finished THEN
                LEAVE run;
            END IF;
            EXECUTE IMMEDIATE next_statement;
        END LOOP;
        CLOSE statements;
    END;

    DROP TEMPORARY TABLE `migration_162_statements`;
    DROP TABLE `migration_162_keys`;
END IF//
DELIMITER ;

DROP TEMPORARY TABLE `migration_162_columns`;
