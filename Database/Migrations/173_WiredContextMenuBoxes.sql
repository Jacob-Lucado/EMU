-- The context menu wired boxes Habbo has shown but not shipped: User Clicks Context Menu and the Context Menu Item add-on.
-- Plus gives them furniture rows without furnidata (has_furnidata = 0). They borrow the look of the placeholder furni
-- wf_test_26secret6 and 7 (sprites 19515 and 19516), which have assets. Each box gets one offer, sold on every Wired page of
-- its kind in both catalogues, priced like the page: triggers 5, add-ons 5. A hotel without those pages gets the rows only.
-- Idempotent: reruns end in the same state.

START TRANSACTION;

CREATE TEMPORARY TABLE wired_boxes_173 (
    item_name VARCHAR(70) NOT NULL PRIMARY KEY,
    sprite_id INT NOT NULL,
    name VARCHAR(56) NOT NULL,
    description VARCHAR(255) NOT NULL,
    interaction VARCHAR(25) NOT NULL,
    page_link VARCHAR(128) NOT NULL,
    cost_credits INT UNSIGNED NOT NULL,
    sort_order INT NOT NULL,
    furniture_id INT UNSIGNED NULL,
    offer_id INT NULL
) ENGINE=MEMORY;
INSERT INTO wired_boxes_173 (item_name, sprite_id, name, description, interaction, page_link, cost_credits, sort_order) VALUES
    ('wf_trg_click_context_menu', 19515, 'WIRED Trigger: User Clicks Context Menu', 'Triggers when a user clicks a context menu button', 'wired_trigger', 'wired_triggers', 5, 1),
    ('wf_xtra_context_menu_item', 19516, 'WIRED Add-on: Context Menu Item', 'Adds a button to the context menu of users', 'wired_addon', 'wired_addons', 5, 1);

INSERT INTO furniture (item_name, public_name, type, width, length, stack_height, can_stack, can_sit, is_walkable, sprite_id,
    allow_recycle, allow_trade, allow_marketplace_sell, allow_gift, allow_inventory_stack, interaction_type, interaction_modes_count,
    has_furnidata, revision, category, default_dir, xdim, ydim, name, description, special_type,
    can_stand_on, can_sit_on, can_lay_on, can_put_stuff_on, height, furni_line, rare, tradeable, recyclable)
SELECT w.item_name, w.name, 's', 1, 1, 0.1, 1, 0, 0, w.sprite_id,
    1, 1, 1, 1, 1, w.interaction, 1,
    0, 76189, 'wired', 0, 1, 1, w.name, w.description, 1,
    0, 0, 0, 1, 0.1, 'wired', 0, 1, 1
FROM wired_boxes_173 w
WHERE NOT EXISTS (SELECT 1 FROM furniture f WHERE f.type = 's' AND f.item_name = w.item_name);

UPDATE wired_boxes_173 w SET w.furniture_id = (SELECT MIN(f.id) FROM furniture f WHERE f.type = 's' AND f.item_name = w.item_name);

-- One offer per box: an offer that already sells the box is kept.
UPDATE wired_boxes_173 w SET w.offer_id = (
    SELECT MIN(op.offer_id) FROM catalog_offer_products op
    JOIN catalog_offers o ON o.id = op.offer_id AND o.localization_key = w.item_name
    WHERE op.product_type = 'furni' AND op.furniture_id = w.furniture_id);

SET @wired_offer_floor_173 = (SELECT COALESCE(MAX(id), 0) FROM catalog_offers);
INSERT INTO catalog_offers (localization_key, cost_credits, cost_points, points_type, club_level, bulk_purchase, enabled, preview_image)
SELECT w.item_name, w.cost_credits, 0, 0, 0, 1, 1, ''
FROM wired_boxes_173 w
WHERE w.offer_id IS NULL AND EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = w.page_link)
ORDER BY w.page_link, w.sort_order;

UPDATE wired_boxes_173 w
JOIN catalog_offers o ON o.id > @wired_offer_floor_173 AND o.localization_key = w.item_name
SET w.offer_id = o.id
WHERE w.offer_id IS NULL;

INSERT INTO catalog_offer_products (offer_id, position, product_type, furniture_id, amount, extra_param)
SELECT w.offer_id, 0, 'furni', w.furniture_id, 1, '' FROM wired_boxes_173 w
WHERE w.offer_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM catalog_offer_products op WHERE op.offer_id = w.offer_id);

-- Every page of the box's kind, in both catalogues, lists the offer at its end.
INSERT INTO catalog_page_offers (page_id, offer_id, position)
SELECT c.id, w.offer_id,
       (SELECT COALESCE(MAX(po.position), -1) FROM catalog_page_offers po WHERE po.page_id = c.id) + w.sort_order
FROM wired_boxes_173 w
JOIN catalog_pages c ON c.link = w.page_link
WHERE w.offer_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM catalog_page_offers po WHERE po.page_id = c.id AND po.offer_id = w.offer_id);

DROP TEMPORARY TABLE wired_boxes_173;

COMMIT;
