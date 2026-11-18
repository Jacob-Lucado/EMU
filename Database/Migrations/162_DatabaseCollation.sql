-- 162_DatabaseCollation
-- One collation for the whole database, as migration 70 set up: the database default, every table and every text
-- column use utf8mb4_unicode_ci. Columns that are deliberately binary (utf8mb4_bin) or ASCII keep their collation.
-- A table created without naming a collation takes the server or database default (utf8mb4_uca1400_ai_ci on
-- MariaDB 11.5 and newer), and string joins and foreign keys between two collations fail.
-- Only tables and columns that differ are changed, so a database that already has one collation is left alone.

ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

SET SESSION group_concat_max_len = 1048576;

CREATE TEMPORARY TABLE `migration_162_columns` AS
    SELECT c.TABLE_NAME, c.ORDINAL_POSITION,
           CONCAT('MODIFY `', c.COLUMN_NAME, '` ', c.COLUMN_TYPE, ' CHARACTER SET utf8mb4 COLLATE ',
                  IF(c.COLLATION_NAME LIKE '%\_bin', 'utf8mb4_bin', 'utf8mb4_unicode_ci'),
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

-- MariaDB will not change the collation of a column in a foreign key, so those keys come off and go back unchanged.
-- They stay in a real table until they are back, so a run that stops halfway restores them the next time.
CREATE TABLE IF NOT EXISTS `migration_162_keys` (
    `table_name` VARCHAR(64) NOT NULL,
    `constraint_name` VARCHAR(64) NOT NULL,
    `addition` TEXT NOT NULL,
    PRIMARY KEY (`table_name`, `constraint_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `migration_162_keys` (`table_name`, `constraint_name`, `addition`)
    SELECT r.TABLE_NAME, r.CONSTRAINT_NAME,
           CONCAT('ADD CONSTRAINT `', r.CONSTRAINT_NAME, '` FOREIGN KEY (',
                  GROUP_CONCAT(CONCAT('`', k.COLUMN_NAME, '`') ORDER BY k.ORDINAL_POSITION), ') REFERENCES `', r.REFERENCED_TABLE_NAME, '` (',
                  GROUP_CONCAT(CONCAT('`', k.REFERENCED_COLUMN_NAME, '`') ORDER BY k.ORDINAL_POSITION), ') ON DELETE ', r.DELETE_RULE,
                  ' ON UPDATE ', r.UPDATE_RULE)
    FROM information_schema.REFERENTIAL_CONSTRAINTS r
    JOIN information_schema.KEY_COLUMN_USAGE k
      ON k.CONSTRAINT_SCHEMA = r.CONSTRAINT_SCHEMA AND k.TABLE_NAME = r.TABLE_NAME AND k.CONSTRAINT_NAME = r.CONSTRAINT_NAME
    LEFT JOIN information_schema.COLUMNS c
      ON c.TABLE_SCHEMA = k.TABLE_SCHEMA AND c.TABLE_NAME = k.TABLE_NAME AND c.COLUMN_NAME = k.COLUMN_NAME
    LEFT JOIN information_schema.COLUMNS p
      ON p.TABLE_SCHEMA = k.TABLE_SCHEMA AND p.TABLE_NAME = k.REFERENCED_TABLE_NAME AND p.COLUMN_NAME = k.REFERENCED_COLUMN_NAME
    WHERE r.CONSTRAINT_SCHEMA = DATABASE()
    GROUP BY r.TABLE_NAME, r.CONSTRAINT_NAME, r.REFERENCED_TABLE_NAME, r.DELETE_RULE, r.UPDATE_RULE
    HAVING SUM(c.CHARACTER_SET_NAME IS NOT NULL AND c.CHARACTER_SET_NAME <> 'ascii' AND c.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin')
            OR p.CHARACTER_SET_NAME IS NOT NULL AND p.CHARACTER_SET_NAME <> 'ascii' AND p.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin')) > 0;

CREATE TEMPORARY TABLE `migration_162_statements` (
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `statement` LONGTEXT NOT NULL
);

INSERT INTO `migration_162_statements` (`statement`)
    SELECT CONCAT('ALTER TABLE `', k.`table_name`, '` ', GROUP_CONCAT(CONCAT('DROP FOREIGN KEY `', k.`constraint_name`, '`') SEPARATOR ', '))
    FROM `migration_162_keys` k
    WHERE EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r
                  WHERE r.CONSTRAINT_SCHEMA = DATABASE() AND r.TABLE_NAME = k.`table_name` AND r.CONSTRAINT_NAME = k.`constraint_name`)
    GROUP BY k.`table_name`;

INSERT INTO `migration_162_statements` (`statement`)
    SELECT CONCAT('ALTER TABLE `', TABLE_NAME, '` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci, ',
                  GROUP_CONCAT(`definition` ORDER BY ORDINAL_POSITION SEPARATOR ', '))
    FROM `migration_162_columns` GROUP BY TABLE_NAME;

INSERT INTO `migration_162_statements` (`statement`)
    SELECT CONCAT('ALTER TABLE `', t.TABLE_NAME, '` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci')
    FROM information_schema.TABLES t
    WHERE t.TABLE_SCHEMA = DATABASE() AND t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_COLLATION <> 'utf8mb4_unicode_ci'
      AND NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS c
                      WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME AND c.CHARACTER_SET_NAME IS NOT NULL
                        AND c.CHARACTER_SET_NAME <> 'ascii' AND c.COLLATION_NAME NOT IN ('utf8mb4_unicode_ci', 'utf8mb4_bin'));

INSERT INTO `migration_162_statements` (`statement`)
    SELECT CONCAT('ALTER TABLE `', `table_name`, '` ', GROUP_CONCAT(`addition` SEPARATOR ', '))
    FROM `migration_162_keys` GROUP BY `table_name`;

SET @migration_162_checks = @@FOREIGN_KEY_CHECKS;
SET FOREIGN_KEY_CHECKS = 0;

DELIMITER //
BEGIN NOT ATOMIC
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
END//
DELIMITER ;

SET FOREIGN_KEY_CHECKS = @migration_162_checks;

DROP TEMPORARY TABLE `migration_162_statements`;
DROP TABLE `migration_162_keys`;
DROP TEMPORARY TABLE `migration_162_columns`;
