-- The group and context menu wired boxes of migrations 172 and 173 get their own sprite ids (normally 9000000 to 9000004) and
-- furnidata rows with their own names, so every client names them. The furnidata still loads the look of the placeholder
-- furni wf_test_26secret3 to 7 (UnreleasedHabboFeatures.BorrowedLooks). The placeholder rows do not change.
-- Idempotent: reruns end in the same state.

START TRANSACTION;

CREATE TEMPORARY TABLE wired_box_sprites_174 (
    item_name VARCHAR(70) NOT NULL PRIMARY KEY,
    sprite_id INT NOT NULL
) ENGINE=MEMORY;
INSERT INTO wired_box_sprites_174 (item_name, sprite_id) VALUES
    ('wf_act_invite_to_group', 9000000),
    ('wf_act_remove_from_group', 9000001),
    ('wf_trg_group_membership', 9000002),
    ('wf_trg_click_context_menu', 9000003),
    ('wf_xtra_context_menu_item', 9000004);

-- Use the first five free ids in the custom range. Unpublished aliases also use their sprite id in packets.
SET @wired_start_174 = (
    WITH occupied AS (
        SELECT 8999999 AS sprite_id
        UNION SELECT sprite_id FROM furniture f WHERE sprite_id >= 9000000 AND NOT EXISTS (
            SELECT 1 FROM wired_box_sprites_174 w WHERE f.type = 's' AND f.item_name = w.item_name)
        UNION SELECT 2147483648
    ), gaps AS (
        SELECT sprite_id, LEAD(sprite_id) OVER (ORDER BY sprite_id) AS next_sprite FROM occupied
    )
    SELECT MIN(sprite_id + 1) FROM gaps WHERE next_sprite - sprite_id > 5
);
UPDATE wired_box_sprites_174 SET sprite_id = sprite_id - 9000000 + @wired_start_174;

UPDATE furniture f
JOIN wired_box_sprites_174 w ON w.item_name = f.item_name
SET f.sprite_id = w.sprite_id
WHERE f.type = 's';

-- One furnidata row per classname: the lowest id, like the stock rows.
UPDATE furniture f
JOIN (
    SELECT MIN(b.id) AS id FROM furniture b JOIN wired_box_sprites_174 w ON w.item_name = b.item_name
    WHERE b.type = 's' GROUP BY b.item_name
) first_box ON first_box.id = f.id
SET f.has_furnidata = 1;

-- A listed box shows the furni of its new sprite id.
UPDATE catalog_marketplace_offers o
JOIN furniture f ON f.id = o.item_id
JOIN wired_box_sprites_174 w ON w.item_name = f.item_name
SET o.sprite_id = w.sprite_id
WHERE f.type = 's';

DROP TEMPORARY TABLE wired_box_sprites_174;

COMMIT;
