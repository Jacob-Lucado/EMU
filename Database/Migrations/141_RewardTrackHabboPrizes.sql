-- Reward track prizes use Habbo's format: product_item_type_id picks the product (0 wall furni, 1 floor furni, 4 badge,
-- 8 currency) and reward_type holds its sprite id, badge code or currency type (-1 credits).
-- Converts rows written with the old reward words. Idempotent.
UPDATE reward_track_prizes SET product_item_type_id = 4, reward_type = TRIM(extra_params), extra_params = ''
WHERE TRIM(reward_type) = 'badge' AND TRIM(extra_params) <> '';

UPDATE reward_track_prizes SET product_item_type_id = 8,
    reward_type = CASE TRIM(reward_type) WHEN 'credits' THEN '-1' WHEN 'duckets' THEN '0' ELSE '5' END
WHERE TRIM(reward_type) IN ('credits', 'duckets', 'diamonds');

UPDATE reward_track_prizes p
JOIN (SELECT item_name, MIN(id) AS id FROM furniture WHERE type IN ('s', 'i') GROUP BY item_name) named ON named.item_name = TRIM(p.extra_params)
JOIN furniture f ON f.id = named.id
SET p.product_item_type_id = IF(f.type = 'i', 0, 1), p.reward_type = f.sprite_id, p.extra_params = ''
WHERE TRIM(p.reward_type) = 'furni';

ALTER TABLE reward_track_prizes ALTER COLUMN reward_type SET DEFAULT '';
