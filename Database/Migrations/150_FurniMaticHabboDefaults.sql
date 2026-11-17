-- A hotel that never set up its Furni-Matic gets Habbo's machine: 8 slots, 10 Duckets, 10 seconds between exchanges and
-- Habbo's prize table. Without every prize furni it only gets the 8 slots. A hotel with its own setup is left unchanged.
SET @recycler_untouched = (SELECT COUNT(*) FROM recycler_levels) = 0 AND (SELECT COUNT(*) FROM recycler_prizes) = 0
    AND COALESCE((SELECT enabled FROM recycler_settings WHERE id = 1), FALSE) = FALSE;

CREATE TEMPORARY TABLE recycler_habbo_levels (level INT NOT NULL PRIMARY KEY, chance INT NOT NULL);
INSERT INTO recycler_habbo_levels(level, chance) VALUES (1, 1), (2, 4), (3, 10), (4, 40), (5, 400), (6, 5000);

CREATE TEMPORARY TABLE recycler_habbo_prizes (level INT NOT NULL, item_name VARCHAR(100) NOT NULL, chat_style_id INT NULL);
INSERT INTO recycler_habbo_prizes(level, item_name, chat_style_id) VALUES
    (1, 'rec26_paintsplatter', NULL), (1, 'rec26_paintcans', NULL), (1, 'rec26_artsupplies', NULL),
    (2, 'rec26_paintcupboard', NULL), (2, 'rec26_paintchair', NULL), (2, 'rec26_painttable', NULL), (2, 'rec26_paintlamp', NULL),
    (3, 'rec26_paintshelf', NULL), (3, 'rec26_paintbust', NULL), (3, 'rec26_paintcouch', NULL),
    (4, 'rec26_paintcolumn', NULL), (4, 'rec26_paintwallart', NULL),
    (5, 'rec26_painteasel', NULL), (5, 'rec26_cloth_painthat', NULL),
    (6, 'rec26_cloth_paintoutfit', NULL), (6, '', 10001);

SET @recycler_furni_complete = (
    SELECT COUNT(*) FROM recycler_habbo_prizes p
    WHERE p.chat_style_id IS NULL AND NOT EXISTS (
        SELECT 1 FROM furniture f WHERE BINARY f.item_name = BINARY p.item_name AND f.type IN ('s', 'i') AND f.sprite_id > 0)) = 0;
SET @recycler_seed = @recycler_untouched AND @recycler_furni_complete;

-- A new settings row, and a machine nobody set up, start with Habbo's 8 slots. A hotel's own setup keeps its slot count.
ALTER TABLE recycler_settings ALTER COLUMN slots SET DEFAULT 8;
UPDATE recycler_settings SET slots = 8 WHERE id = 1 AND slots = 5 AND @recycler_untouched;

-- The paint-splattered speech bubble is a purchasable style that only an opened box grants.
INSERT IGNORE INTO room_chat_styles(id, enabled, requires_hc, requires_ownership)
SELECT 10001, TRUE, FALSE, TRUE FROM DUAL WHERE @recycler_seed;

INSERT INTO recycler_levels(level, chance) SELECT level, chance FROM recycler_habbo_levels WHERE @recycler_seed;

INSERT INTO recycler_prizes(level, item_id)
SELECT p.level, MIN(f.id) FROM recycler_habbo_prizes p
JOIN furniture f ON BINARY f.item_name = BINARY p.item_name AND f.type IN ('s', 'i') AND f.sprite_id > 0
WHERE @recycler_seed AND p.chat_style_id IS NULL
GROUP BY p.level, p.item_name;

INSERT INTO recycler_prizes(level, item_id, chat_style_id)
SELECT level, NULL, chat_style_id FROM recycler_habbo_prizes WHERE @recycler_seed AND chat_style_id IS NOT NULL;

INSERT INTO recycler_settings(id, enabled, slots, cooldown_seconds, cost_duckets)
SELECT 1, TRUE, 8, 10, 10 FROM DUAL WHERE @recycler_seed
ON DUPLICATE KEY UPDATE enabled = TRUE, slots = 8, cooldown_seconds = 10, cost_duckets = 10;

DROP TEMPORARY TABLE recycler_habbo_prizes;
DROP TEMPORARY TABLE recycler_habbo_levels;
