-- The owner can allow other users to breed with a pet. Idempotent.
ALTER TABLE `bots_petdata` ADD COLUMN IF NOT EXISTS `allow_breeding` tinyint(1) NOT NULL DEFAULT 0 AFTER `anyone_ride`;
