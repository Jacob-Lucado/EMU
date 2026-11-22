-- Keep the parent keys in place so icon ownership and foreign keys stay valid.
DROP TEMPORARY TABLE IF EXISTS migration_163_sets;
CREATE TEMPORARY TABLE migration_163_sets AS
SELECT id, CASE id WHEN 5 THEN 7 WHEN 6 THEN 8 WHEN 7 THEN 5 WHEN 8 THEN 6 END AS target_id,
       name, reward_id, cost_credits, cost_points, points_type
FROM habbicon_collections
WHERE (id, reward_id) IN ((5, 60), (6, 71), (7, 38), (8, 49));

SET @old_sets = (SELECT COUNT(*) FROM migration_163_sets);

-- Swap the set data and membership in one InnoDB statement so a retry sees either complete layout.
UPDATE habbicon_collections c
JOIN migration_163_sets s ON s.target_id = c.id
LEFT JOIN habbicons h ON h.collection_id = c.id
SET c.name = s.name, c.reward_id = s.reward_id, c.cost_credits = s.cost_credits,
    c.cost_points = s.cost_points, c.points_type = s.points_type, h.collection_id = s.id
WHERE @old_sets = 4;

DROP TEMPORARY TABLE migration_163_sets;
