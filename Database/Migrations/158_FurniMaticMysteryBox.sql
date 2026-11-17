-- The Furni-Matic gives Habbo's Mystery Box, which cannot be recycled, traded or sold on the Marketplace. Idempotent.
UPDATE furniture SET allow_recycle = 0, allow_trade = 0, allow_marketplace_sell = 0
WHERE BINARY item_name = BINARY 'matic_box' AND sprite_id = 4692;
