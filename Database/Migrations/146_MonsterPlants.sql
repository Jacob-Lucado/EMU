-- Monster plant state, one row per plant pet (pet type 16). Idempotent.
CREATE TABLE IF NOT EXISTS `bots_monsterplants` (
  `pet_id` INT(10) UNSIGNED NOT NULL,
  `rarity` INT(11) NOT NULL DEFAULT 0,
  `growth_started_at` DATETIME(6) NOT NULL,
  `wellbeing_until` DATETIME(6) NOT NULL,
  `can_breed` TINYINT(1) NOT NULL DEFAULT 1,
  `breeding_permission` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`pet_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
