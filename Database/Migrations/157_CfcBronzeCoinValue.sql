-- The bronze CFC_ coin is worth 10 credits in its furni asset. It cannot be redeemed, but Wired chest payments
-- count that value and refuse a coin worth 0. Idempotent.
UPDATE furniture SET behaviour_data = 10 WHERE item_name = 'CFC_10_coin_bronze' AND interaction_type = 'exchange' AND behaviour_data <= 0;
