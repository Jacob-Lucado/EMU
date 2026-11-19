-- Only the Toast Habbicons are for sale, just like Habbo: the set costs 50 credits and 50 diamonds, one icon 5 credits and 5 diamonds.
-- The Duck, Duck 2 and Frank Habbicons are only earned. A price that a hotel changed is kept. Idempotent.
UPDATE habbicon_collections SET cost_credits = 50, cost_points = 50, points_type = 5
WHERE name = 'toast' AND cost_credits = 40 AND cost_points = 0 AND points_type = 0;

UPDATE habbicon_collections SET cost_credits = 0
WHERE name IN ('duck', 'duck2', 'frank') AND cost_credits = 40 AND cost_points = 0 AND points_type = 0;

UPDATE habbicons h JOIN habbicon_collections c ON c.id = h.collection_id
SET h.cost_points = 5, h.points_type = 5
WHERE c.name = 'toast' AND h.id <> c.reward_id AND h.cost_credits = 5 AND h.cost_points = 0 AND h.points_type = 0;

UPDATE habbicons h JOIN habbicon_collections c ON c.id = h.collection_id
SET h.cost_credits = 0
WHERE c.name IN ('duck', 'duck2', 'frank') AND h.id <> c.reward_id AND h.cost_credits = 5 AND h.cost_points = 0 AND h.points_type = 0;
