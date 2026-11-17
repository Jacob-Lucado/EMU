-- The furni the landing view offers as the bonus rare, and the credits to buy for one. An empty item name hides it. Idempotent.
INSERT IGNORE INTO server_settings (`key`, `value`) VALUES ('landing.bonus_rare.item_name', 'bonusbag26_3'), ('landing.bonus_rare.credits', '120');
