-- A listed group furni keeps its group on the offer, so a cancel or a purchase links it to the group again. Idempotent.
ALTER TABLE catalog_marketplace_offers ADD COLUMN IF NOT EXISTS group_id INT UNSIGNED NOT NULL DEFAULT 0;
