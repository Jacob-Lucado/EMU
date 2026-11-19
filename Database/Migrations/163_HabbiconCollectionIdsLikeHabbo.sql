-- Habbicon collections use Habbo's ids: 5 duck, 6 duck2, 7 frank, 8 toast. The client reads the collection icons by these ids.
-- Nothing changes unless all four collections still have the old ids. Safe to run again, also after an interrupted run.
SET @old_ids = (SELECT COUNT(*) FROM habbicon_collections WHERE (id, name) IN ((5, 'frank'), (6, 'toast'), (7, 'duck'), (8, 'duck2')));

INSERT IGNORE INTO habbicon_collections (id, name, reward_id, cost_credits, cost_points, points_type)
SELECT id + 1000, name, reward_id, cost_credits, cost_points, points_type FROM habbicon_collections
WHERE @old_ids = 4 AND id BETWEEN 5 AND 8;

UPDATE habbicons SET collection_id = collection_id + 1000
WHERE @old_ids = 4 AND collection_id BETWEEN 5 AND 8;

DELETE FROM habbicon_collections WHERE @old_ids = 4 AND id BETWEEN 5 AND 8;

INSERT IGNORE INTO habbicon_collections (id, name, reward_id, cost_credits, cost_points, points_type)
SELECT CASE name WHEN 'duck' THEN 5 WHEN 'duck2' THEN 6 WHEN 'frank' THEN 7 WHEN 'toast' THEN 8 END,
       name, reward_id, cost_credits, cost_points, points_type
FROM habbicon_collections WHERE id BETWEEN 1005 AND 1008;

UPDATE habbicons h JOIN habbicon_collections c ON c.id = h.collection_id
SET h.collection_id = CASE c.name WHEN 'duck' THEN 5 WHEN 'duck2' THEN 6 WHEN 'frank' THEN 7 WHEN 'toast' THEN 8 END
WHERE h.collection_id BETWEEN 1005 AND 1008;

DELETE FROM habbicon_collections WHERE id BETWEEN 1005 AND 1008;
