-- Habbo's zombie Habbicons (collection 9, icons 72-81 and the collection reward 82) are Habboween prizes. They cannot be bought. Idempotent.
INSERT IGNORE INTO habbicon_collections (id, name, reward_id, cost_credits, cost_points, points_type) VALUES
(9, 'zombie', 82, 0, 0, 0);

INSERT IGNORE INTO habbicons (id, collection_id, name, cost_credits, cost_points, points_type, available, default_owned) VALUES
(72, 9, 'zombie_zombie', 0, 0, 0, 1, 0),
(73, 9, 'zombie_glee', 0, 0, 0, 1, 0),
(74, 9, 'zombie_dizzy', 0, 0, 0, 1, 0),
(75, 9, 'zombie_hungry', 0, 0, 0, 1, 0),
(76, 9, 'zombie_rage', 0, 0, 0, 1, 0),
(77, 9, 'zombie_terror', 0, 0, 0, 1, 0),
(78, 9, 'zombie_panic', 0, 0, 0, 1, 0),
(79, 9, 'zombie_melt', 0, 0, 0, 1, 0),
(80, 9, 'zombie_nobrain', 0, 0, 0, 1, 0),
(81, 9, 'zombie_bandage', 0, 0, 0, 1, 0),
(82, 9, 'zombie_whack', 0, 0, 0, 1, 0);
