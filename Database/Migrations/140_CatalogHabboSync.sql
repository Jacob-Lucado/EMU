-- Adds what Habbo sells and this hotel lacks, as Habbo lists it, in both catalogues.
-- - New pages and the offers on them, in Habbo's order. Pages Habbo hides (early access) sit under Staff and need the administrator permission.
-- - Offers Habbo lists on a page this hotel already has. A new offer gets Habbo's offer id, key and prices.
-- - A Habbo offer id this hotel already has is only linked to the page, when the key and the products match.
-- - Furniture with no public Habbo offer gets a price 0 offer on its furni line page under Staff > Other (the page is made when missing).
-- Furniture is found by item name. Migration 139 adds the new furniture rows, so it must run first: an offer whose furniture
-- is missing is skipped, and so is every Staff offer for a missing row. Existing offers and pages are not changed.
-- Idempotent: reruns end in the same state.

START TRANSACTION;

INSERT IGNORE INTO badge_definitions (code, required_right) VALUES
    ('WSH10', ''),
    ('WSH15', '');

-- 1. Pages

-- NORMAL: EARLY ACCESS: Survival Game Weapons
-- NORMAL nov26_weaponcase: hidden on Habbo, so it sits under Staff.
INSERT INTO catalog_pages (parent_id, caption, icon, required_permission, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT st.id, 'EARLY ACCESS: Survival Game Weapons', 263, 'catalog.pages.administrator', (SELECT COALESCE(MAX(c.position), -1) + 1 FROM catalog_pages c WHERE c.parent_id = st.id), 'nov26_weaponcase', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages st WHERE st.link = 'staff' AND st.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26_weaponcase' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_Ingredients2_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26_weaponcase' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Survival tip #1: be armed before everyone else. Survival tip #2: don’t pay full price. This early access bundle covers both – all six items on display, at a discount, with an exclusive badge. Bought individually, they’ll arrive gradually until early December. You’ve got one week.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26_weaponcase' AND c.catalog_mode = 'NORMAL';
-- NORMAL: NEW: Wilderness Survival Game Furni
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL'
SET c.position = c.position + 1 WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'NEW: Wilderness Survival Game Furni', 363, 0, 'nov26gamefurni', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_GameItems_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'You are now the game master. Let the games begin.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Tactical Supplies
-- NORMAL nov26crackb: hidden on Habbo, so it sits under Staff.
INSERT INTO catalog_pages (parent_id, caption, icon, required_permission, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT st.id, 'Tactical Supplies', 196, 'catalog.pages.administrator', (SELECT COALESCE(MAX(c.position), -1) + 1 FROM catalog_pages c WHERE c.parent_id = st.id), 'nov26crackb', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages st WHERE st.link = 'staff' AND st.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26crackb' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_Ingredients_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26crackb' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'To survive in this environment, you’ll need some basic supplies. Looking for an upgrade? Start crafting.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26crackb' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Survival Game Weapons
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'nov26gamefurni' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26clothcrackb' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Survival Game Weapons', 196, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'nov26gamefurni' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'nov26clothcrackb', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26clothcrackb' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_nov26_weapon' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26clothcrackb' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'If you''re going to fight to the death, you will need a weapon.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26clothcrackb' AND c.catalog_mode = 'NORMAL';
-- NORMAL: NEW: Wilderness Survival Furni
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'nov26clothcrackb' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26furni' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'NEW: Wilderness Survival Furni', 233, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'nov26clothcrackb' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'nov26furni', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26furni' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, '' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26furni' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Surviving in the wilderness just got easier with this brand new furni collection. But not too easy.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26furni' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Survivalist Crafting Table
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'nov26furni' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26craft' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Survivalist Crafting Table', 323, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'nov26furni' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'nov26craft', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26craft' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_CraftingTable_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26craft' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'If you want to survive, you need to get crafty. Transform your crafting ingredients into something more useful.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26craft' AND c.catalog_mode = 'NORMAL';
-- NORMAL: NEW: Seasonal Currency Items!
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'nov26craft' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'hween26sc' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'NEW: Seasonal Currency Items!', 300, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'nov26craft' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'hween26sc', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'hween26sc' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_sc_letter' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'hween26sc' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Get Letters via the Reward Track to spend on the Alchemy Tower set from the Shop. Note: you will need to convert all items into Relics and have all 4 in your MetaMask or Collector Wallet by 12/11 to get your bonus Wizard Tower Nook from the Collectors Guild!' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'hween26sc' AND c.catalog_mode = 'NORMAL';
-- NORMAL: RARE Loser’s Coffin
-- NORMAL nov26r2: hidden on Habbo, so it sits under Staff.
INSERT INTO catalog_pages (parent_id, caption, icon, required_permission, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT st.id, 'RARE Loser’s Coffin', 42, 'catalog.pages.administrator', (SELECT COALESCE(MAX(c.position), -1) + 1 FROM catalog_pages c WHERE c.parent_id = st.id), 'nov26r2', 'default_3x3', 'NORMAL', 1, 1, 0
FROM catalog_pages st WHERE st.link = 'staff' AND st.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26r2' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_LosersCoffin_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26r2' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Proud to be a loser.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26r2' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Ancients Mystery Bundle
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL'
SET c.position = c.position + 1 WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'anc_jungle_bundle2' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Ancients Mystery Bundle', 268, 0, 'anc_jungle_bundle2', 'single_bundle', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'anc_jungle_bundle2' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_roombundle' AS image UNION ALL SELECT 1 AS slot, 'jun15_ancients_internal2' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'anc_jungle_bundle2' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Deep in the heart of Habbo Jungle, hidden amongst a mystical clearing in the swamp, you''ll find ancient relics that have survived through the centuries. Includes Badge!' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '<i><b><font color="#ba3733" size="16">What''s Included:</b></i> ' AS text UNION ALL SELECT 3 AS slot, '' AS text) v
WHERE c.link = 'anc_jungle_bundle2' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Ancients Mystery Bundle
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'anc_jungle_bundle2' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'anc_jungle_bundle' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Ancients Mystery Bundle', 268, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'anc_jungle_bundle2' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'anc_jungle_bundle', 'single_bundle', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'anc_jungle_bundle' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_roombundle' AS image UNION ALL SELECT 1 AS slot, 'jun15_ancients_internal2' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'anc_jungle_bundle' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Deep in the heart of Habbo Jungle, hidden amongst a mystical clearing in the swamp, you''ll find ancient relics that have survived through the centuries. Includes Badge!' AS text UNION ALL SELECT 1 AS slot, 'Also available in the Credit page. ' AS text UNION ALL SELECT 2 AS slot, '<i><b><font color="#ba3733" size="16">What''s Included:</b></i> ' AS text UNION ALL SELECT 3 AS slot, '<a href="event:habblet/open/credits">Buy direct and save more than 50%</a> ' AS text) v
WHERE c.link = 'anc_jungle_bundle' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Military Camp Bundle
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'anc_jungle_bundle' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'apr18army' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Military Camp Bundle', 268, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'anc_jungle_bundle' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'apr18army', 'single_bundle', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'apr18army' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_roombundle' AS image UNION ALL SELECT 1 AS slot, 'apr18_movie2_internal' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'apr18army' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Including 2x Action Hero Outfits (a bandana and bullet chain), this bundle is SERIOUSLY not one to miss out on.' AS text UNION ALL SELECT 1 AS slot, 'Also available in the Credit page. ' AS text UNION ALL SELECT 2 AS slot, '<i><b><font color="#ba3733" size="16">What''s Included:</b></i> ' AS text UNION ALL SELECT 3 AS slot, '<a href="event:habblet/open/credits">Buy direct and save more than 50%</a> ' AS text) v
WHERE c.link = 'apr18army' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Military Camp Bundle
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'apr18army' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'apr18army2' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Military Camp Bundle', 268, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'apr18army' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'apr18army2', 'single_bundle', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_roombundles' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'apr18army2' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_roombundle' AS image UNION ALL SELECT 1 AS slot, 'apr18_movie2_internal' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'apr18army2' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Including 2x Action Hero Outfits (a bandana and bullet chain), this bundle is SERIOUSLY not one to miss out on.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '<i><b><font color="#ba3733" size="16">What''s Included:</b></i>' AS text UNION ALL SELECT 3 AS slot, '' AS text) v
WHERE c.link = 'apr18army2' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Furni-Matic
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'furni' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'marketplace_info' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_transform' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Furni-Matic', 163, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'marketplace_info' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'ecotron_transform', 'recycler', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'furni' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_transform' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_furnimatic' AS image UNION ALL SELECT 1 AS slot, 'ctlg_furnimatic_image' AS image) v
WHERE c.link = 'ecotron_transform' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Recycle your worthless stuff and be rewarded with a random prize.  Check out the prizes and instructions for recycling.  Drag a furni to each box below and click recycle!' AS text) v
WHERE c.link = 'ecotron_transform' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Instructions
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'ecotron_transform' AND par.catalog_mode = 'NORMAL'
SET c.position = c.position + 1 WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_info' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Instructions', 42, 0, 'ecotron_info', 'recycler_info', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'ecotron_transform' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_info' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_furnimatic' AS image UNION ALL SELECT 1 AS slot, 'ctlg_ecotron_box' AS image) v
WHERE c.link = 'ecotron_info' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'The Furni-Matic is a Furni Recycling machine! Feed your old Recyclable Furni into the machine and get a shiny new Furni in return.' AS text UNION ALL SELECT 1 AS slot, 'Using the Furni-Matic' AS text UNION ALL SELECT 2 AS slot, '1. Keep the main Furni-Matic window open while clicking the ''My Stuff'' button. You''ll see that most of your items have recycling symbols on them, this means you can click and drag them into the Furni-Matic.. \n2. When you have enough items in there, click "Start" and look for the Furni-Matic box in your inventory. \n3. Click the box to see its tag. Open the box, or trade it unopened. The timer shows you how long you have to wait before you can feed more items to the Furni-Matic. P.S. Check out the Rewards before you recycle - don''t be surprised by the surprise!' AS text) v
WHERE c.link = 'ecotron_info' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Rewards
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'ecotron_transform' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'ecotron_info' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_rewards' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Rewards', 51, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'ecotron_info' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'ecotron_rewards', 'recycler_prizes', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'ecotron_transform' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'ecotron_rewards' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_header_furnimatic' AS image) v
WHERE c.link = 'ecotron_rewards' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Feed your recyclable Furni into the Furni-Matic and you''ll receive one of the items below. But which one...?' AS text) v
WHERE c.link = 'ecotron_rewards' AND c.catalog_mode = 'NORMAL';
-- NORMAL: White Pigeon
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'pets_info' AND par.catalog_mode = 'NORMAL'
SET c.position = c.position + 1 WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_pigeon_white' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'White Pigeon', 160, 0, 'pet_pigeon_white', 'pets', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'pets_info' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_pigeon_white' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_pet_headline1' AS image UNION ALL SELECT 1 AS slot, '' AS image) v
WHERE c.link = 'pet_pigeon_white' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'The White Pigeon is wise and true. It has flown all across the world and has the wisdom of a sage, or someone who has listened to too many self-help cassettes.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, 'Name Your pigeon:' AS text UNION ALL SELECT 3 AS slot, '' AS text) v
WHERE c.link = 'pet_pigeon_white' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Black Pigeon
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'pets_info' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'pet_pigeon_white' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_pigeon_black' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Black Pigeon', 160, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'pet_pigeon_white' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'pet_pigeon_black', 'pets', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'pets_info' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_pigeon_black' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_pet_headline1' AS image UNION ALL SELECT 1 AS slot, '' AS image) v
WHERE c.link = 'pet_pigeon_black' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'The black pigeon has traveled the world, much like the white pigeon, but has failed to learn anything. This pet might be considered dangerous, if it wasn''t so foolish...' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, 'Name your pet' AS text UNION ALL SELECT 3 AS slot, '' AS text) v
WHERE c.link = 'pet_pigeon_black' AND c.catalog_mode = 'NORMAL';
-- NORMAL: Bad Bunny
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = 'pets_info' AND par.catalog_mode = 'NORMAL'
JOIN catalog_pages sp ON sp.link = 'pet_pigeon_black' AND sp.catalog_mode = 'NORMAL' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_bunnyblack' AND c.catalog_mode = 'NORMAL');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'Bad Bunny', 151, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'pet_pigeon_black' AND sp.catalog_mode = 'NORMAL'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'pet_bunnyblack', 'pets', 'NORMAL', 1, 1, 0
FROM catalog_pages par WHERE par.link = 'pets_info' AND par.catalog_mode = 'NORMAL' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'pet_bunnyblack' AND c.catalog_mode = 'NORMAL');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'catalog_pet_headline1' AS image UNION ALL SELECT 1 AS slot, 'black_bunny_promo' AS image) v
WHERE c.link = 'pet_bunnyblack' AND c.catalog_mode = 'NORMAL';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'This bunny is full of bad attitude. Nearly impossible to train and pretty mean, adopt this little critter at your peril.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, 'Name your pet:' AS text UNION ALL SELECT 3 AS slot, '' AS text) v
WHERE c.link = 'pet_bunnyblack' AND c.catalog_mode = 'NORMAL';
-- BUILDERS_CLUB: NEW: Wilderness Survival Game Furni
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'BUILDERS_CLUB'
SET c.position = c.position + 1 WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'BUILDERS_CLUB');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'NEW: Wilderness Survival Game Furni', 363, 0, 'nov26gamefurni', 'default_3x3', 'BUILDERS_CLUB', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'BUILDERS_CLUB' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'BUILDERS_CLUB');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, 'spromo_GameItems_Nov26' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'BUILDERS_CLUB';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'You are now the game master. Let the games begin.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26gamefurni' AND c.catalog_mode = 'BUILDERS_CLUB';
-- BUILDERS_CLUB: NEW: Wilderness Survival Furni
UPDATE catalog_pages c JOIN catalog_pages par ON par.id = c.parent_id AND par.link = '2020_hotnow' AND par.catalog_mode = 'BUILDERS_CLUB'
JOIN catalog_pages sp ON sp.link = 'nov26gamefurni' AND sp.catalog_mode = 'BUILDERS_CLUB' AND sp.parent_id = par.id
SET c.position = c.position + 1 WHERE c.position > sp.position AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26furni' AND c.catalog_mode = 'BUILDERS_CLUB');
INSERT INTO catalog_pages (parent_id, caption, icon, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT par.id, 'NEW: Wilderness Survival Furni', 233, COALESCE((SELECT sp.position + 1 FROM catalog_pages sp WHERE sp.parent_id = par.id AND sp.link = 'nov26gamefurni' AND sp.catalog_mode = 'BUILDERS_CLUB'), (SELECT COALESCE(MAX(c2.position), -1) + 1 FROM catalog_pages c2 WHERE c2.parent_id = par.id)), 'nov26furni', 'default_3x3', 'BUILDERS_CLUB', 1, 1, 0
FROM catalog_pages par WHERE par.link = '2020_hotnow' AND par.catalog_mode = 'BUILDERS_CLUB' AND NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.link = 'nov26furni' AND c.catalog_mode = 'BUILDERS_CLUB');
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, v.slot, v.image FROM catalog_pages c
JOIN (SELECT 0 AS slot, '' AS image UNION ALL SELECT 1 AS slot, '' AS image UNION ALL SELECT 2 AS slot, '' AS image) v
WHERE c.link = 'nov26furni' AND c.catalog_mode = 'BUILDERS_CLUB';
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, v.slot, v.text FROM catalog_pages c
JOIN (SELECT 0 AS slot, 'Surviving in the wilderness just got easier with this brand new furni collection. But not too easy.' AS text UNION ALL SELECT 1 AS slot, '' AS text UNION ALL SELECT 2 AS slot, '' AS text) v
WHERE c.link = 'nov26furni' AND c.catalog_mode = 'BUILDERS_CLUB';

-- 2. Offers Habbo lists that this hotel lacks.
CREATE TEMPORARY TABLE offer_plan_140 (
    offer_id INT NOT NULL PRIMARY KEY,
    offer_key VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    cost_credits INT UNSIGNED NOT NULL,
    cost_points INT UNSIGNED NOT NULL,
    points_type INT UNSIGNED NOT NULL,
    club_level TINYINT UNSIGNED NOT NULL,
    bulk_purchase TINYINT NOT NULL,
    preview_image VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    ok TINYINT NOT NULL DEFAULT 1,
    needs_products TINYINT NOT NULL DEFAULT 0
) ENGINE=MEMORY;
CREATE TEMPORARY TABLE product_plan_140 (
    offer_id INT NOT NULL,
    position INT NOT NULL,
    product_type VARCHAR(16) CHARACTER SET ascii NOT NULL,
    furniture_type CHAR(1) CHARACTER SET ascii NULL,
    item_name VARCHAR(70) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    badge_code VARCHAR(35) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    number_value INT NULL,
    amount INT UNSIGNED NOT NULL,
    furniture_id INT UNSIGNED NULL,
    PRIMARY KEY (offer_id, position)
) ENGINE=MEMORY;
INSERT INTO offer_plan_140 (offer_id, offer_key, cost_credits, cost_points, points_type, club_level, bulk_purchase, preview_image) VALUES
    (11058, 'a0 pet18', 66, 0, 0, 0, 1, ''),
    (11196, 'a0 pet21', 5, 0, 0, 0, 1, ''),
    (11197, 'a0 pet22', 5, 0, 0, 0, 1, ''),
    (14070, 'st_wildwest_tnt', 4, 0, 0, 0, 1, ''),
    (20255, 'rotating_roomdeal207_d', 45, 45, 5, 0, 0, ''),
    (20256, 'rotating_roomdeal207', 99, 0, 0, 0, 0, 'catalogue/apr18_movie2_internal.gif'),
    (26449, 'rotating_roomdeal66', 99, 0, 0, 0, 0, 'catalogue/jun15_ancients_internal2.gif'),
    (33922, 'CF_350_emergencyduck', 350, 10, 5, 0, 0, ''),
    (34940, 'sc26_alchemykit', 0, 250, 104, 0, 1, ''),
    (34941, 'sc26_flyingbook', 0, 100, 104, 0, 1, ''),
    (34942, 'sc26_transmogrification', 0, 150, 104, 0, 1, ''),
    (34943, 'sc26_wanddisplay', 0, 500, 104, 0, 1, ''),
    (35026, 'clothing_acornbackpack', 0, 1500, 0, 0, 1, ''),
    (35027, 'clothing_acornkeyring', 0, 250, 0, 0, 1, ''),
    (35028, 'clothing_autumnberet2', 0, 1000, 0, 0, 1, ''),
    (35029, 'ducket_c26_acorndecor', 0, 100, 0, 0, 1, ''),
    (35043, 'survival_c26_burntgrass', 4, 0, 0, 0, 1, ''),
    (35045, 'survival_c26_craftingtable', 9, 10, 5, 0, 1, ''),
    (35046, 'survival_c26_dystopiafence', 5, 0, 0, 0, 1, ''),
    (35047, 'survival_c26_dystopiagate', 6, 0, 0, 0, 1, ''),
    (35051, 'survival_c26_floortarget', 3, 0, 0, 0, 1, ''),
    (35056, 'survival_c26_knife', 2, 0, 0, 0, 1, ''),
    (35057, 'survival_c26_medpack', 3, 10, 5, 0, 1, ''),
    (35064, 'survival_c26_supplycrateb', 5, 10, 5, 0, 1, ''),
    (35069, 'survival_c26_weaponcaseb', 5, 10, 5, 0, 1, ''),
    (35079, 'survival_cloth_c26_weapon', 15, 15, 5, 0, 1, ''),
    (35084, 'survival_c26_dystopiabg', 5, 10, 5, 0, 1, ''),
    (35085, 'survival_c26_dystopiawall', 4, 0, 0, 0, 1, ''),
    (35086, 'survival_c26_walltarget', 3, 0, 0, 0, 1, ''),
    (35249, 'wf_act_send_dataframe', 7, 0, 0, 0, 1, ''),
    (35250, 'wf_act_send_message', 5, 0, 0, 0, 1, ''),
    (35254, 'wf_link', 8, 0, 0, 0, 1, ''),
    (35255, 'wf_trg_receive_dataframe', 7, 0, 0, 0, 1, ''),
    (35256, 'wf_trg_receive_message', 5, 0, 0, 0, 1, ''),
    (35261, 'wf_xtra_link_speaker', 4, 0, 0, 0, 1, ''),
    (35300, 'dia_c26_acorndecor', 0, 2, 5, 0, 0, ''),
    (35301, 'dia_c26_acornkeyring', 0, 4, 5, 0, 0, ''),
    (35302, 'dia_c26_autumnberet2', 0, 9, 5, 0, 0, ''),
    (35303, 'dia_c26_acornbackpack', 0, 15, 5, 0, 0, ''),
    (35398, 'supply_pack3', 9, 10, 5, 0, 0, ''),
    (35399, 'supply_pack4', 9, 10, 5, 0, 0, ''),
    (35400, 'supply_pack1', 9, 10, 5, 0, 0, ''),
    (35401, 'supply_pack2', 9, 10, 5, 0, 0, ''),
    (35402, 'supply_pack5', 9, 10, 5, 0, 0, ''),
    (35429, 'rotating_roomdeal66_d', 45, 45, 5, 0, 0, '');
INSERT INTO product_plan_140 (offer_id, position, product_type, furniture_type, item_name, badge_code, number_value, amount) VALUES
    (11058, 0, 'pet', NULL, NULL, NULL, 18, 1),
    (11196, 0, 'pet', NULL, NULL, NULL, 21, 1),
    (11197, 0, 'pet', NULL, NULL, NULL, 22, 1),
    (14070, 0, 'furni', 's', 'st_wildwest_tnt', NULL, NULL, 1),
    (20255, 0, 'badge', NULL, NULL, 'WSH15', NULL, 1),
    (20255, 1, 'furni', 's', 'env_tree1', NULL, NULL, 1),
    (20255, 2, 'furni', 's', 'army_c15_gate', NULL, NULL, 1),
    (20255, 3, 'furni', 's', 'african_tree1', NULL, NULL, 1),
    (20255, 4, 'furni', 's', 'jungle_c16_tree', NULL, NULL, 1),
    (20255, 5, 'furni', 's', 'country_ditch', NULL, NULL, 4),
    (20255, 6, 'furni', 's', 'clothing_parade', NULL, NULL, 1),
    (20255, 7, 'furni', 's', 'army_c15_table', NULL, NULL, 1),
    (20255, 8, 'furni', 's', 'pirate_cannon', NULL, NULL, 1),
    (20255, 9, 'furni', 's', 'country_corner', NULL, NULL, 4),
    (20255, 10, 'furni', 's', 'roombg_stars', NULL, NULL, 1),
    (20255, 11, 'furni', 's', 'clothing_mercenary', NULL, NULL, 2),
    (20255, 12, 'furni', 's', 'clothing_kevlaroutfit', NULL, NULL, 1),
    (20255, 13, 'furni', 's', 'army_c15_deskitem', NULL, NULL, 1),
    (20255, 14, 'furni', 's', 'army_c15_deskcomp', NULL, NULL, 1),
    (20255, 15, 'furni', 's', 'pirate_cannonballs', NULL, NULL, 1),
    (20255, 16, 'furni', 's', 'army_c15_guardcub', NULL, NULL, 1),
    (20255, 17, 'furni', 's', 'army_c15_wiredividr', NULL, NULL, 5),
    (20255, 18, 'furni', 's', 'army_c15_jerrycan', NULL, NULL, 2),
    (20255, 19, 'furni', 's', 'tiki_junglerug', NULL, NULL, 4),
    (20255, 20, 'furni', 's', 'army_c15_watchtwr', NULL, NULL, 1),
    (20255, 21, 'furni', 's', 'wildwest_floor', NULL, NULL, 2),
    (20255, 22, 'furni', 's', 'army_c15_sandbag', NULL, NULL, 6),
    (20255, 23, 'furni', 's', 'dino_c15_tree3', NULL, NULL, 1),
    (20255, 24, 'furni', 's', 'val15_tree', NULL, NULL, 1),
    (20255, 25, 'furni', 's', 'army_c15_wirecrn', NULL, NULL, 2),
    (20255, 26, 'furni', 's', 'army_c15_shipcon', NULL, NULL, 1),
    (20255, 27, 'furni', 's', 'army_c15_gunrack', NULL, NULL, 2),
    (20255, 28, 'furni', 's', 'clothing_camooutfit', NULL, NULL, 1),
    (20255, 29, 'furni', 's', 'army_c15_medictent', NULL, NULL, 1),
    (20255, 30, 'furni', 's', 'army_c15_bed', NULL, NULL, 1),
    (20256, 0, 'badge', NULL, NULL, 'WSH15', NULL, 1),
    (20256, 1, 'furni', 's', 'env_tree1', NULL, NULL, 1),
    (20256, 2, 'furni', 's', 'army_c15_gate', NULL, NULL, 1),
    (20256, 3, 'furni', 's', 'african_tree1', NULL, NULL, 1),
    (20256, 4, 'furni', 's', 'jungle_c16_tree', NULL, NULL, 1),
    (20256, 5, 'furni', 's', 'country_ditch', NULL, NULL, 4),
    (20256, 6, 'furni', 's', 'clothing_parade', NULL, NULL, 1),
    (20256, 7, 'furni', 's', 'army_c15_table', NULL, NULL, 1),
    (20256, 8, 'furni', 's', 'pirate_cannon', NULL, NULL, 1),
    (20256, 9, 'furni', 's', 'country_corner', NULL, NULL, 4),
    (20256, 10, 'furni', 's', 'roombg_stars', NULL, NULL, 1),
    (20256, 11, 'furni', 's', 'clothing_mercenary', NULL, NULL, 2),
    (20256, 12, 'furni', 's', 'clothing_kevlaroutfit', NULL, NULL, 1),
    (20256, 13, 'furni', 's', 'army_c15_deskitem', NULL, NULL, 1),
    (20256, 14, 'furni', 's', 'army_c15_deskcomp', NULL, NULL, 1),
    (20256, 15, 'furni', 's', 'pirate_cannonballs', NULL, NULL, 1),
    (20256, 16, 'furni', 's', 'army_c15_guardcub', NULL, NULL, 1),
    (20256, 17, 'furni', 's', 'army_c15_wiredividr', NULL, NULL, 5),
    (20256, 18, 'furni', 's', 'army_c15_jerrycan', NULL, NULL, 2),
    (20256, 19, 'furni', 's', 'tiki_junglerug', NULL, NULL, 4),
    (20256, 20, 'furni', 's', 'army_c15_watchtwr', NULL, NULL, 1),
    (20256, 21, 'furni', 's', 'wildwest_floor', NULL, NULL, 2),
    (20256, 22, 'furni', 's', 'army_c15_sandbag', NULL, NULL, 6),
    (20256, 23, 'furni', 's', 'dino_c15_tree3', NULL, NULL, 1),
    (20256, 24, 'furni', 's', 'val15_tree', NULL, NULL, 1),
    (20256, 25, 'furni', 's', 'army_c15_wirecrn', NULL, NULL, 2),
    (20256, 26, 'furni', 's', 'army_c15_shipcon', NULL, NULL, 1),
    (20256, 27, 'furni', 's', 'army_c15_gunrack', NULL, NULL, 2),
    (20256, 28, 'furni', 's', 'clothing_camooutfit', NULL, NULL, 1),
    (20256, 29, 'furni', 's', 'army_c15_medictent', NULL, NULL, 1),
    (20256, 30, 'furni', 's', 'army_c15_bed', NULL, NULL, 1),
    (26449, 0, 'badge', NULL, NULL, 'WSH10', NULL, 1),
    (26449, 1, 'furni', 's', 'shallow', NULL, NULL, 14),
    (26449, 2, 'furni', 's', 'anc_talltree', NULL, NULL, 3),
    (26449, 3, 'furni', 's', 'anc_mossyrock', NULL, NULL, 7),
    (26449, 4, 'furni', 's', 'anc_trophy_sstone', NULL, NULL, 1),
    (26449, 5, 'furni', 's', 'anc_comfy_tree', NULL, NULL, 1),
    (26449, 6, 'furni', 's', 'anc_waterfall', NULL, NULL, 2),
    (26449, 7, 'furni', 's', 'anc_jngl_patch', NULL, NULL, 16),
    (26449, 8, 'furni', 's', 'pink_flamingo', NULL, NULL, 3),
    (26449, 9, 'furni', 's', 'anc_ele_grass', NULL, NULL, 5),
    (26449, 10, 'furni', 's', 'anc_ele_rock', NULL, NULL, 8),
    (26449, 11, 'furni', 'i', 'anc_sunset_wall', NULL, NULL, 6),
    (26449, 12, 'furni', 's', 'anc_bones', NULL, NULL, 1),
    (33922, 0, 'furni', 's', 'CF_350_emergencyduck', NULL, NULL, 1),
    (34940, 0, 'furni', 's', 'sc26_alchemykit', NULL, NULL, 1),
    (34941, 0, 'furni', 's', 'sc26_flyingbook', NULL, NULL, 1),
    (34942, 0, 'furni', 's', 'sc26_transmogrification', NULL, NULL, 1),
    (34943, 0, 'furni', 's', 'sc26_wanddisplay', NULL, NULL, 1),
    (35026, 0, 'furni', 's', 'clothing_acornbackpack', NULL, NULL, 1),
    (35027, 0, 'furni', 's', 'clothing_acornkeyring', NULL, NULL, 1),
    (35028, 0, 'furni', 's', 'clothing_autumnberet2', NULL, NULL, 1),
    (35029, 0, 'furni', 's', 'ducket_c26_acorndecor', NULL, NULL, 1),
    (35043, 0, 'furni', 's', 'survival_c26_burntgrass', NULL, NULL, 1),
    (35045, 0, 'furni', 's', 'survival_c26_craftingtable', NULL, NULL, 1),
    (35046, 0, 'furni', 's', 'survival_c26_dystopiafence', NULL, NULL, 1),
    (35047, 0, 'furni', 's', 'survival_c26_dystopiagate', NULL, NULL, 1),
    (35051, 0, 'furni', 's', 'survival_c26_floortarget', NULL, NULL, 1),
    (35056, 0, 'furni', 's', 'survival_c26_knife', NULL, NULL, 1),
    (35057, 0, 'furni', 's', 'survival_c26_medpack', NULL, NULL, 1),
    (35064, 0, 'furni', 's', 'survival_c26_supplycrateb', NULL, NULL, 1),
    (35069, 0, 'furni', 's', 'survival_c26_weaponcaseb', NULL, NULL, 1),
    (35079, 0, 'furni', 's', 'survival_cloth_c26_weapon', NULL, NULL, 1),
    (35084, 0, 'furni', 'i', 'survival_c26_dystopiabg', NULL, NULL, 1),
    (35085, 0, 'furni', 'i', 'survival_c26_dystopiawall', NULL, NULL, 1),
    (35086, 0, 'furni', 'i', 'survival_c26_walltarget', NULL, NULL, 1),
    (35249, 0, 'furni', 's', 'wf_act_send_dataframe', NULL, NULL, 1),
    (35250, 0, 'furni', 's', 'wf_act_send_message', NULL, NULL, 1),
    (35254, 0, 'furni', 's', 'wf_link', NULL, NULL, 1),
    (35255, 0, 'furni', 's', 'wf_trg_receive_dataframe', NULL, NULL, 1),
    (35256, 0, 'furni', 's', 'wf_trg_receive_message', NULL, NULL, 1),
    (35261, 0, 'furni', 's', 'wf_xtra_link_speaker', NULL, NULL, 1),
    (35300, 0, 'furni', 's', 'ducket_c26_acorndecor', NULL, NULL, 1),
    (35301, 0, 'furni', 's', 'clothing_acornkeyring', NULL, NULL, 1),
    (35302, 0, 'furni', 's', 'clothing_autumnberet2', NULL, NULL, 1),
    (35303, 0, 'furni', 's', 'clothing_acornbackpack', NULL, NULL, 1),
    (35398, 0, 'furni', 's', 'survival_c26_stonesymbol', NULL, NULL, 1),
    (35398, 1, 'furni', 's', 'survival_c26_firekit', NULL, NULL, 1),
    (35398, 2, 'furni', 's', 'survival_c26_rope', NULL, NULL, 1),
    (35399, 0, 'furni', 's', 'survival_c26_foodsupplies', NULL, NULL, 1),
    (35399, 1, 'furni', 's', 'survival_c26_sleepingbag', NULL, NULL, 1),
    (35399, 2, 'furni', 's', 'survival_c26_firekit', NULL, NULL, 1),
    (35400, 0, 'furni', 's', 'survival_c26_stonesymbol', NULL, NULL, 1),
    (35400, 1, 'furni', 's', 'survival_c26_rope', NULL, NULL, 1),
    (35400, 2, 'furni', 's', 'survival_c26_sleepingbag', NULL, NULL, 1),
    (35401, 0, 'furni', 's', 'survival_c26_foodsupplies', NULL, NULL, 1),
    (35401, 1, 'furni', 's', 'survival_c26_firekit', NULL, NULL, 1),
    (35401, 2, 'furni', 's', 'survival_c26_stonesymbol', NULL, NULL, 1),
    (35402, 0, 'furni', 's', 'survival_c26_rope', NULL, NULL, 1),
    (35402, 1, 'furni', 's', 'survival_c26_sleepingbag', NULL, NULL, 1),
    (35402, 2, 'furni', 's', 'survival_c26_foodsupplies', NULL, NULL, 1),
    (35429, 0, 'badge', NULL, NULL, 'WSH10', NULL, 1),
    (35429, 1, 'furni', 's', 'shallow', NULL, NULL, 14),
    (35429, 2, 'furni', 's', 'anc_talltree', NULL, NULL, 3),
    (35429, 3, 'furni', 's', 'anc_mossyrock', NULL, NULL, 7),
    (35429, 4, 'furni', 's', 'anc_trophy_sstone', NULL, NULL, 1),
    (35429, 5, 'furni', 's', 'anc_comfy_tree', NULL, NULL, 1),
    (35429, 6, 'furni', 's', 'anc_waterfall', NULL, NULL, 2),
    (35429, 7, 'furni', 's', 'anc_jngl_patch', NULL, NULL, 16),
    (35429, 8, 'furni', 's', 'pink_flamingo', NULL, NULL, 3),
    (35429, 9, 'furni', 's', 'anc_ele_grass', NULL, NULL, 5),
    (35429, 10, 'furni', 's', 'anc_ele_rock', NULL, NULL, 8),
    (35429, 11, 'furni', 'i', 'anc_sunset_wall', NULL, NULL, 6),
    (35429, 12, 'furni', 's', 'anc_bones', NULL, NULL, 1);

UPDATE product_plan_140 p JOIN furniture f ON f.item_name = p.item_name AND f.type = p.furniture_type SET p.furniture_id = f.id WHERE p.product_type = 'furni';
-- One missing furniture row or badge definition drops the whole offer.
UPDATE offer_plan_140 o SET o.ok = 0 WHERE EXISTS (
    SELECT 1 FROM product_plan_140 p WHERE p.offer_id = o.offer_id AND (
        (p.product_type = 'furni' AND p.furniture_id IS NULL)
        OR (p.product_type = 'badge' AND NOT EXISTS (SELECT 1 FROM badge_definitions b WHERE b.code = p.badge_code))));

INSERT INTO catalog_offers (id, localization_key, cost_credits, cost_points, points_type, club_level, bulk_purchase, enabled, preview_image)
SELECT o.offer_id, o.offer_key, o.cost_credits, o.cost_points, o.points_type, o.club_level, o.bulk_purchase, 1, o.preview_image
FROM offer_plan_140 o
WHERE o.ok = 1 AND NOT EXISTS (SELECT 1 FROM catalog_offers x WHERE x.id = o.offer_id);

-- Only an offer of this plan with no products yet gets them: a rerun, or an id that another offer took, adds nothing.
UPDATE offer_plan_140 o JOIN catalog_offers x ON x.id = o.offer_id AND x.localization_key = o.offer_key
SET o.needs_products = 1
WHERE o.ok = 1 AND NOT EXISTS (SELECT 1 FROM catalog_offer_products op WHERE op.offer_id = o.offer_id);

INSERT INTO catalog_offer_products (offer_id, position, product_type, furniture_id, effect_id, badge_code, pet_type, amount, extra_param)
SELECT p.offer_id, p.position, p.product_type, p.furniture_id,
       IF(p.product_type = 'effect', p.number_value, NULL), p.badge_code, IF(p.product_type = 'pet', p.number_value, NULL), p.amount, ''
FROM product_plan_140 p JOIN offer_plan_140 o ON o.offer_id = p.offer_id AND o.needs_products = 1;

-- 3. Offers on pages.
CREATE TEMPORARY TABLE link_plan_140 (
    catalog_mode VARCHAR(16) CHARACTER SET ascii NOT NULL,
    page_link VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    offer_id INT NOT NULL,
    offer_key VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    habbo_index INT NOT NULL,
    habbo_rank INT NOT NULL,
    placement VARCHAR(8) CHARACTER SET ascii NOT NULL,
    page_id INT NULL,
    ok TINYINT NOT NULL DEFAULT 0,
    linked TINYINT NOT NULL DEFAULT 0,
    PRIMARY KEY (catalog_mode, page_link, offer_id)
) ENGINE=MEMORY;
INSERT INTO link_plan_140 (catalog_mode, page_link, offer_id, offer_key, habbo_index, habbo_rank, placement) VALUES
    ('NORMAL', 'credit_exchange', 33922, 'CF_350_emergencyduck', 0, 0, 'insert'),
    ('NORMAL', 'nov26gamefurni', 35056, 'survival_c26_knife', 0, 0, 'new'),
    ('NORMAL', 'nov26gamefurni', 35057, 'survival_c26_medpack', 1, 1, 'new'),
    ('NORMAL', 'nov26gamefurni', 35051, 'survival_c26_floortarget', 2, 2, 'new'),
    ('NORMAL', 'nov26gamefurni', 35086, 'survival_c26_walltarget', 3, 3, 'new'),
    ('NORMAL', 'nov26crackb', 35064, 'survival_c26_supplycrateb', 0, 0, 'new'),
    ('NORMAL', 'nov26crackb', 35400, 'supply_pack1', 1, 1, 'new'),
    ('NORMAL', 'nov26crackb', 35401, 'supply_pack2', 2, 2, 'new'),
    ('NORMAL', 'nov26crackb', 35398, 'supply_pack3', 3, 3, 'new'),
    ('NORMAL', 'nov26crackb', 35399, 'supply_pack4', 4, 4, 'new'),
    ('NORMAL', 'nov26crackb', 35402, 'supply_pack5', 5, 5, 'new'),
    ('NORMAL', 'nov26clothcrackb', 35069, 'survival_c26_weaponcaseb', 0, 0, 'new'),
    ('NORMAL', 'nov26clothcrackb', 35079, 'survival_cloth_c26_weapon', 1, 1, 'new'),
    ('NORMAL', 'nov26furni', 35085, 'survival_c26_dystopiawall', 0, 0, 'new'),
    ('NORMAL', 'nov26furni', 35046, 'survival_c26_dystopiafence', 1, 1, 'new'),
    ('NORMAL', 'nov26furni', 35047, 'survival_c26_dystopiagate', 2, 2, 'new'),
    ('NORMAL', 'nov26furni', 35084, 'survival_c26_dystopiabg', 3, 3, 'new'),
    ('NORMAL', 'nov26furni', 35043, 'survival_c26_burntgrass', 4, 4, 'new'),
    ('NORMAL', 'nov26craft', 35045, 'survival_c26_craftingtable', 0, 0, 'new'),
    ('NORMAL', 'hween26sc', 34941, 'sc26_flyingbook', 0, 0, 'new'),
    ('NORMAL', 'hween26sc', 34940, 'sc26_alchemykit', 1, 1, 'new'),
    ('NORMAL', 'hween26sc', 34942, 'sc26_transmogrification', 2, 2, 'new'),
    ('NORMAL', 'hween26sc', 34943, 'sc26_wanddisplay', 3, 3, 'new'),
    ('NORMAL', 'ducket_furni', 35029, 'ducket_c26_acorndecor', 0, 0, 'insert'),
    ('NORMAL', 'ducket_furni', 35027, 'clothing_acornkeyring', 1, 1, 'insert'),
    ('NORMAL', 'ducket_furni', 35028, 'clothing_autumnberet2', 2, 2, 'insert'),
    ('NORMAL', 'ducket_furni', 35026, 'clothing_acornbackpack', 3, 3, 'insert'),
    ('NORMAL', 'ducket_diamonds', 35300, 'dia_c26_acorndecor', 0, 0, 'insert'),
    ('NORMAL', 'ducket_diamonds', 35301, 'dia_c26_acornkeyring', 1, 1, 'insert'),
    ('NORMAL', 'ducket_diamonds', 35302, 'dia_c26_autumnberet2', 2, 2, 'insert'),
    ('NORMAL', 'ducket_diamonds', 35303, 'dia_c26_acornbackpack', 3, 3, 'insert'),
    ('NORMAL', 'pet_acc_ducket', 12283, 'milkbowl', 0, 0, 'insert'),
    ('NORMAL', 'anc_jungle_bundle2', 35429, 'rotating_roomdeal66_d', 0, 0, 'new'),
    ('NORMAL', 'anc_jungle_bundle', 26449, 'rotating_roomdeal66', 0, 0, 'new'),
    ('NORMAL', 'apr18army', 20256, 'rotating_roomdeal207', 0, 0, 'new'),
    ('NORMAL', 'apr18army2', 20255, 'rotating_roomdeal207_d', 0, 0, 'new'),
    ('NORMAL', 'wired_new', 35254, 'wf_link', 0, 0, 'insert'),
    ('NORMAL', 'wired_new', 35256, 'wf_trg_receive_message', 1, 1, 'insert'),
    ('NORMAL', 'wired_new', 35250, 'wf_act_send_message', 2, 2, 'insert'),
    ('NORMAL', 'wired_new', 35255, 'wf_trg_receive_dataframe', 3, 3, 'insert'),
    ('NORMAL', 'wired_new', 35249, 'wf_act_send_dataframe', 4, 4, 'insert'),
    ('NORMAL', 'wired_new', 35261, 'wf_xtra_link_speaker', 5, 5, 'insert'),
    ('NORMAL', 'wired_triggers', 35256, 'wf_trg_receive_message', 21, 0, 'insert'),
    ('NORMAL', 'wired_triggers', 35255, 'wf_trg_receive_dataframe', 22, 1, 'insert'),
    ('NORMAL', 'wired_effects', 35250, 'wf_act_send_message', 37, 0, 'insert'),
    ('NORMAL', 'wired_effects', 35249, 'wf_act_send_dataframe', 38, 1, 'insert'),
    ('NORMAL', 'wired_extras', 35254, 'wf_link', 7, 0, 'insert'),
    ('NORMAL', 'wired_extras', 35261, 'wf_xtra_link_speaker', 8, 1, 'insert'),
    ('NORMAL', 'pet_pigeon_white', 11196, 'a0 pet21', 0, 0, 'new'),
    ('NORMAL', 'pet_pigeon_black', 11197, 'a0 pet22', 0, 0, 'new'),
    ('NORMAL', 'pet_bunnyblack', 11058, 'a0 pet18', 0, 0, 'new'),
    ('BUILDERS_CLUB', 'nov26gamefurni', 35056, 'survival_c26_knife', 0, 0, 'new'),
    ('BUILDERS_CLUB', 'nov26gamefurni', 35057, 'survival_c26_medpack', 1, 1, 'new'),
    ('BUILDERS_CLUB', 'nov26gamefurni', 35051, 'survival_c26_floortarget', 2, 2, 'new'),
    ('BUILDERS_CLUB', 'nov26gamefurni', 35086, 'survival_c26_walltarget', 3, 3, 'new'),
    ('BUILDERS_CLUB', 'nov26furni', 35085, 'survival_c26_dystopiawall', 0, 0, 'new'),
    ('BUILDERS_CLUB', 'nov26furni', 35046, 'survival_c26_dystopiafence', 1, 1, 'new'),
    ('BUILDERS_CLUB', 'nov26furni', 35047, 'survival_c26_dystopiagate', 2, 2, 'new'),
    ('BUILDERS_CLUB', 'nov26furni', 35084, 'survival_c26_dystopiabg', 3, 3, 'new'),
    ('BUILDERS_CLUB', 'nov26furni', 35043, 'survival_c26_burntgrass', 4, 4, 'new'),
    ('BUILDERS_CLUB', 'dynamic_product_category_chair', 8782, 'bw_sofa_p', 113, 0, 'insert'),
    ('BUILDERS_CLUB', 'set_pool', 8782, 'bw_sofa_p', 43, 0, 'insert'),
    ('BUILDERS_CLUB', 'bc_wildwest', 14070, 'st_wildwest_tnt', 39, 0, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35254, 'wf_link', 0, 0, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35256, 'wf_trg_receive_message', 1, 1, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35250, 'wf_act_send_message', 2, 2, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35255, 'wf_trg_receive_dataframe', 3, 3, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35249, 'wf_act_send_dataframe', 4, 4, 'insert'),
    ('BUILDERS_CLUB', 'wired_new', 35261, 'wf_xtra_link_speaker', 5, 5, 'insert'),
    ('BUILDERS_CLUB', 'wired_triggers', 35256, 'wf_trg_receive_message', 21, 0, 'insert'),
    ('BUILDERS_CLUB', 'wired_triggers', 35255, 'wf_trg_receive_dataframe', 22, 1, 'insert'),
    ('BUILDERS_CLUB', 'wired_effects', 35250, 'wf_act_send_message', 37, 0, 'insert'),
    ('BUILDERS_CLUB', 'wired_effects', 35249, 'wf_act_send_dataframe', 38, 1, 'insert'),
    ('BUILDERS_CLUB', 'wired_extras', 35254, 'wf_link', 6, 0, 'insert'),
    ('BUILDERS_CLUB', 'wired_extras', 35261, 'wf_xtra_link_speaker', 7, 1, 'insert');

UPDATE link_plan_140 l JOIN catalog_pages p ON p.link = l.page_link AND p.catalog_mode = l.catalog_mode SET l.page_id = p.id;
-- The offer must exist with the planned key and sell something.
UPDATE link_plan_140 l SET l.ok = 1 WHERE l.page_id IS NOT NULL
  AND EXISTS (SELECT 1 FROM catalog_offers o WHERE o.id = l.offer_id AND o.localization_key = l.offer_key)
  AND EXISTS (SELECT 1 FROM catalog_offer_products op WHERE op.offer_id = l.offer_id);
UPDATE link_plan_140 l SET l.linked = 1 WHERE EXISTS (SELECT 1 FROM catalog_page_offers po WHERE po.page_id = l.page_id AND po.offer_id = l.offer_id);

-- A new page gets its offers in Habbo's order.
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, l.habbo_rank FROM link_plan_140 l WHERE l.ok = 1 AND l.linked = 0 AND l.placement = 'new';

-- A page with many missing offers gets them after the offers it has.
CREATE TEMPORARY TABLE append_base_140 (page_id INT NOT NULL PRIMARY KEY, base INT NOT NULL) ENGINE=MEMORY;
INSERT INTO append_base_140 (page_id, base)
SELECT l.page_id, COALESCE((SELECT MAX(po.position) + 1 FROM catalog_page_offers po WHERE po.page_id = l.page_id), 0)
FROM link_plan_140 l WHERE l.ok = 1 AND l.linked = 0 AND l.placement = 'append' GROUP BY l.page_id;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, b.base + l.habbo_rank
FROM link_plan_140 l JOIN append_base_140 b ON b.page_id = l.page_id
WHERE l.ok = 1 AND l.linked = 0 AND l.placement = 'append';
DROP TEMPORARY TABLE append_base_140;

-- A page with a few missing offers gets each at Habbo's index; the offers from that index on move down by one.
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'bc_wildwest' AND l.offer_id = 14070 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 39;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 39 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'bc_wildwest' AND l.offer_id = 14070 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'dynamic_product_category_chair' AND l.offer_id = 8782 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 113;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 113 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'dynamic_product_category_chair' AND l.offer_id = 8782 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'set_pool' AND l.offer_id = 8782 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 43;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 43 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'set_pool' AND l.offer_id = 8782 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_effects' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 37;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 37 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_effects' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_effects' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 38;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 38 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_effects' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_extras' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 6;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 6 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_extras' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_extras' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 7;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 7 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_extras' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 1;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 1 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 2;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 2 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 3;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 3 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 4;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 4 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 5;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 5 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_new' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_triggers' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 21;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 21 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_triggers' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_triggers' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 22;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 22 FROM link_plan_140 l WHERE l.catalog_mode = 'BUILDERS_CLUB' AND l.page_link = 'wired_triggers' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'credit_exchange' AND l.offer_id = 33922 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'credit_exchange' AND l.offer_id = 33922 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35300 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35300 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35301 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 1;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 1 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35301 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35302 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 2;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 2 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35302 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35303 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 3;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 3 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_diamonds' AND l.offer_id = 35303 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35029 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35029 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35027 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 1;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 1 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35027 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35028 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 2;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 2 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35028 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35026 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 3;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 3 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'ducket_furni' AND l.offer_id = 35026 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'pet_acc_ducket' AND l.offer_id = 12283 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'pet_acc_ducket' AND l.offer_id = 12283 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_effects' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 37;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 37 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_effects' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_effects' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 38;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 38 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_effects' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_extras' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 7;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 7 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_extras' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_extras' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 8;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 8 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_extras' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 0;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 0 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35254 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 1;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 1 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 2;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 2 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35250 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 3;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 3 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 4;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 4 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35249 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 5;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 5 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_new' AND l.offer_id = 35261 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_triggers' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 21;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 21 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_triggers' AND l.offer_id = 35256 AND l.ok = 1 AND l.linked = 0;
UPDATE catalog_page_offers po JOIN link_plan_140 l ON l.page_id = po.page_id AND l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_triggers' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0
SET po.position = po.position + 1 WHERE po.position >= 22;
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT l.page_id, l.offer_id, 22 FROM link_plan_140 l WHERE l.catalog_mode = 'NORMAL' AND l.page_link = 'wired_triggers' AND l.offer_id = 35255 AND l.ok = 1 AND l.linked = 0;

-- 4. Furniture with no public Habbo offer: a price 0 offer per furni line on a Staff page, as the other furni lines have.
CREATE TEMPORARY TABLE staff_plan_140 (
    line VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    item_name VARCHAR(70) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    furniture_type CHAR(1) CHARACTER SET ascii NOT NULL,
    furniture_id INT UNSIGNED NULL,
    page_id INT NULL,
    offer_id INT NULL,
    PRIMARY KEY (line, item_name)
) ENGINE=MEMORY;
INSERT INTO staff_plan_140 (line, item_name, furniture_type) VALUES
    ('configuration', 'conf_kickwars_enable', 's'),
    ('configuration', 'conf_test_26secret8', 's'),
    ('drago', 'drago_cursed_1_package', 's'),
    ('drago', 'drago_cursed_2_package', 's'),
    ('newbie', 'newbie_bronze_cave_dragon_package', 's'),
    ('nft2026', 'clothing_nft26_candycanecane', 's'),
    ('nft2026', 'clothing_nft26_candycanehair', 's'),
    ('nft2026', 'clothing_nft26_duckchestprint', 's'),
    ('nft2026', 'clothing_nft26_duckheadband', 's'),
    ('nft2026', 'clothing_nft26_duckonesie', 's'),
    ('nft2026', 'clothing_nft26_duckslippers', 's'),
    ('nft2026', 'clothing_nft26_nutcrackerface', 's'),
    ('nft2026', 'clothing_nft26_nutcrackerhat', 's'),
    ('nft2026', 'clothing_nft26_penguinheadband', 's'),
    ('nft2026', 'clothing_nft26_polarbearhat', 's'),
    ('nft2026', 'clothing_nft26_snowcoat', 's'),
    ('nft2026', 'clothing_nft26_starchestprint', 's'),
    ('nft2026', 'clothing_nft26_starcrown', 's'),
    ('nft2026', 'clothing_nft26_starfriend', 's'),
    ('nft2026', 'clothing_nft26_starkeyring', 's'),
    ('nft2026', 'nft_emerald_penguin_5000', 's'),
    ('nft2026', 'nft_emerald_tome_5000', 's'),
    ('nft2026', 'nft_h26_candycanecandle', 's'),
    ('nft2026', 'nft_h26_candycanedragonlamp', 's'),
    ('nft2026', 'nft_h26_candycaneheart', 'i'),
    ('nft2026', 'nft_h26_iceskaterack', 's'),
    ('nft2026', 'nft_h26_nutbowl', 's'),
    ('nft2026', 'nft_h26_nutcrackerfrank', 's'),
    ('nft2026', 'nft_h26_santa3000figure', 's'),
    ('nft2026', 'nft_h26_whackapenguin', 's'),
    ('nft2026', 'nft_h26_xmastree_bronze', 's'),
    ('nft2026', 'nft_h26_xmastree_gold', 's'),
    ('nft2026', 'nft_h26_xmastree_silver', 's'),
    ('nft2026', 'nft_h26_xmastree_trippy', 's'),
    ('nft2026', 'nft_sc26_festivepopcorncart', 's'),
    ('nft2026', 'nft_sc26_popcornbucket', 's'),
    ('nft2026', 'nft_sc26_transmogrification', 's'),
    ('nft2026', 'nft_sc26_wanddisplay', 's'),
    ('nft2026', 'nft_sc26_xmasmerch', 's'),
    ('nft2026', 'nft_sc26_xmasshopstand', 's'),
    ('nft2026', 'nft_sc26_xmaswalldisplay', 's'),
    ('nft2026', 'sc26_popcornbucket', 's'),
    ('nft2026', 'sc26_xmasmerch', 's'),
    ('nft2026', 'sc26_xmasshopstand', 's'),
    ('nft2026', 'sc26_xmaswalldisplay', 's'),
    ('nftmint', 'nft_birds_r18_cardinal', 's'),
    ('nftmint', 'nft_birds_r18_crane', 's'),
    ('nftmint', 'nft_birds_r18_quetzal', 's'),
    ('nftmint', 'nft_bling15_pooltable', 's'),
    ('nftmint', 'nft_chair_plasto_lm1', 's'),
    ('nftmint', 'nft_cland15_fountain', 's'),
    ('nftmint', 'nft_diamond_painting14', 'i'),
    ('nftmint', 'nft_diamond_painting16', 'i'),
    ('nftmint', 'nft_diamond_painting19', 'i'),
    ('nftmint', 'nft_diamond_painting2', 'i'),
    ('nftmint', 'nft_diamond_painting23', 'i'),
    ('nftmint', 'nft_diamond_painting26', 'i'),
    ('nftmint', 'nft_diamond_painting29', 'i'),
    ('nftmint', 'nft_diamond_painting4', 'i'),
    ('nftmint', 'nft_diamond_painting5', 'i'),
    ('nftmint', 'nft_jp15_luckycat', 's'),
    ('nftmint', 'nft_paris15_bicycle', 's'),
    ('nftmint', 'nft_paris15_giftsnails', 's'),
    ('nftmint', 'nft_paris15_paintings', 's'),
    ('nftmint', 'nft_st_palooza_screen', 's'),
    ('nftmint', 'nft_st_palooza_unicorn', 's'),
    ('testing', 'greenscreen_background', 's'),
    ('testing', 'greenscreen_floor', 's'),
    ('testing', 'greenscreen_wall', 'i'),
    ('trophies', 'trophy_c26_duckbergine', 's'),
    ('wired', 'wf_test_26secret3', 's'),
    ('wired', 'wf_test_26secret4', 's'),
    ('wired', 'wf_test_26secret5', 's'),
    ('wired', 'wf_test_26secret6', 's'),
    ('wired', 'wf_test_26secret7', 's'),
    ('xmas2026', 'xmas_c26_bluewhitecandle', 's'),
    ('xmas2026', 'xmas_c26_candycanebumpercar', 's'),
    ('xmas2026', 'xmas_c26_candycanelamppost', 's'),
    ('xmas2026', 'xmas_c26_candycanelane', 's'),
    ('xmas2026', 'xmas_c26_candycanelanecurve', 's'),
    ('xmas2026', 'xmas_c26_cupsnowman', 's'),
    ('xmas2026', 'xmas_c26_decorhouse', 's'),
    ('xmas2026', 'xmas_c26_desksnowman', 's'),
    ('xmas2026', 'xmas_c26_ferriswheel', 's'),
    ('xmas2026', 'xmas_c26_giantgingerbreadhouse', 's'),
    ('xmas2026', 'xmas_c26_giftduck', 's'),
    ('xmas2026', 'xmas_c26_gingerbreadcutout', 's'),
    ('xmas2026', 'xmas_c26_gingerbreadcutoutthin', 's'),
    ('xmas2026', 'xmas_c26_gingerbreadplate', 's'),
    ('xmas2026', 'xmas_c26_holidaymocktail', 's'),
    ('xmas2026', 'xmas_c26_monorail', 's'),
    ('xmas2026', 'xmas_c26_monorailcorner', 's'),
    ('xmas2026', 'xmas_c26_parkcottage', 's'),
    ('xmas2026', 'xmas_c26_parkentrance', 's'),
    ('xmas2026', 'xmas_c26_parktoteb', 's'),
    ('xmas2026', 'xmas_c26_parktrashcan', 's'),
    ('xmas2026', 'xmas_c26_reindeerplush', 's'),
    ('xmas2026', 'xmas_c26_reindeerringtoss', 's'),
    ('xmas2026', 'xmas_c26_santaplush', 's'),
    ('xmas2026', 'xmas_c26_snowduck', 's'),
    ('xmas2026', 'xmas_c26_snowglobe', 's'),
    ('xmas2026', 'xmas_c26_stuffedstocking', 'i'),
    ('xmas2026', 'xmas_c26_teacup', 's'),
    ('xmas2026', 'xmas_c26_themeparkbackground', 'i'),
    ('xmas2026', 'xmas_c26_ticketbooth', 's'),
    ('xmas2026', 'xmas_c26_xmasmonorailsleigh', 's'),
    ('xmas2026', 'xmas_c26_xmaspopcorn', 's'),
    ('xmas2026', 'xmas_c26_xmastreeballtoss', 's'),
    ('xmas2026', 'xmas_cloth_c26_friendlygingerbread', 's'),
    ('xmas2026', 'xmas_cloth_c26_gigagiftbow', 's'),
    ('xmas2026', 'xmas_cloth_c26_gingerbreadbackpack', 's'),
    ('xmas2026', 'xmas_cloth_c26_gingerbreaddress', 's'),
    ('xmas2026', 'xmas_cloth_c26_santakeyring', 's'),
    ('xmas2026', 'xmas_cloth_c26_santamask', 's'),
    ('xmas2026', 'xmas_cloth_c26_shortshag', 's'),
    ('xmas2026', 'xmas_cloth_c26_themeparkheadband', 's'),
    ('xmas2026', 'xmas_cloth_c26_winterprincesshair', 's'),
    ('xmas2026', 'xmas_cloth_c26_xmastophat', 's'),
    ('xmas2026', 'xmas_cloth_r26_polarbearcub', 's'),
    ('xmas2026', 'xmas_cloth_r26_reindeerscarf', 's'),
    ('xmas2026', 'xmas_r26_ferriswheel', 's'),
    ('xmas2026', 'xmas_r26_merryarcade', 's'),
    ('xmas2026', 'xmas_r26_minimerrygoround', 's');

UPDATE staff_plan_140 s JOIN furniture f ON f.item_name = s.item_name AND f.type = s.furniture_type SET s.furniture_id = f.id;
-- Rows of furniture that does not exist are dropped, and so are lines left without any.
DELETE FROM staff_plan_140 WHERE furniture_id IS NULL;

INSERT INTO catalog_pages (parent_id, caption, icon, required_permission, position, link, layout, catalog_mode, visible, enabled, required_club_level)
SELECT other.id, CONCAT(UPPER(LEFT(l.line, 1)), SUBSTRING(l.line, 2)), 1, 'catalog.pages.administrator',
    (SELECT COALESCE(MAX(c.position), -1) + 1 FROM catalog_pages c WHERE c.parent_id = other.id), NULL, 'default_3x3', 'NORMAL', 1, 1, 0
FROM (SELECT DISTINCT line FROM staff_plan_140) l
JOIN catalog_pages staff ON staff.link = 'staff' AND staff.catalog_mode = 'NORMAL'
JOIN catalog_pages other ON other.parent_id = staff.id AND other.caption = 'Other'
WHERE NOT EXISTS (SELECT 1 FROM catalog_pages c WHERE c.parent_id IN (other.id, staff.id) AND c.caption = CONCAT(UPPER(LEFT(l.line, 1)), SUBSTRING(l.line, 2)))
ORDER BY l.line;
INSERT IGNORE INTO catalog_page_images (page_id, slot, image)
SELECT c.id, s.slot, IF(s.slot = 0, 'catalog_decorative_header_dyn', '') FROM catalog_pages c
JOIN catalog_pages other ON other.id = c.parent_id AND other.caption = 'Other'
JOIN catalog_pages staff ON staff.id = other.parent_id AND staff.link = 'staff'
JOIN (SELECT DISTINCT CONCAT(UPPER(LEFT(line, 1)), SUBSTRING(line, 2)) AS caption FROM staff_plan_140) l ON l.caption = c.caption
JOIN (SELECT 0 AS slot UNION ALL SELECT 1 UNION ALL SELECT 2) s;
INSERT IGNORE INTO catalog_page_texts (page_id, slot, text)
SELECT c.id, s.slot, '' FROM catalog_pages c
JOIN catalog_pages other ON other.id = c.parent_id AND other.caption = 'Other'
JOIN catalog_pages staff ON staff.id = other.parent_id AND staff.link = 'staff'
JOIN (SELECT DISTINCT CONCAT(UPPER(LEFT(line, 1)), SUBSTRING(line, 2)) AS caption FROM staff_plan_140) l ON l.caption = c.caption
JOIN (SELECT 0 AS slot UNION ALL SELECT 1) s;

-- A page of the line under Other wins over a Staff category of the same name.
UPDATE staff_plan_140 s
JOIN catalog_pages staff ON staff.link = 'staff' AND staff.catalog_mode = 'NORMAL'
JOIN catalog_pages other ON other.parent_id = staff.id AND other.caption = 'Other'
SET s.page_id = COALESCE(
    (SELECT MIN(c.id) FROM catalog_pages c WHERE c.parent_id = other.id AND c.caption = CONCAT(UPPER(LEFT(s.line, 1)), SUBSTRING(s.line, 2))),
    (SELECT MIN(c.id) FROM catalog_pages c WHERE c.parent_id = staff.id AND c.caption = CONCAT(UPPER(LEFT(s.line, 1)), SUBSTRING(s.line, 2))));
-- An offer that already sells the furniture on that page is reused.
UPDATE staff_plan_140 s SET s.offer_id = (
    SELECT MIN(po.offer_id) FROM catalog_page_offers po
    JOIN catalog_offer_products op ON op.offer_id = po.offer_id AND op.product_type = 'furni' AND op.furniture_id = s.furniture_id
    WHERE po.page_id = s.page_id)
WHERE s.page_id IS NOT NULL;

-- New offers get ids above every existing one, which tells them apart from older offers with the same key.
SET @staff_offers_before_140 = (SELECT COALESCE(MAX(id), 0) FROM catalog_offers);
INSERT INTO catalog_offers (localization_key, cost_credits, cost_points, points_type, club_level, bulk_purchase, enabled, preview_image)
SELECT s.item_name, 0, 0, 0, 0, 1, 1, '' FROM staff_plan_140 s
WHERE s.offer_id IS NULL AND s.page_id IS NOT NULL ORDER BY s.line, s.item_name;
UPDATE staff_plan_140 s JOIN catalog_offers o ON o.id > @staff_offers_before_140 AND o.localization_key = s.item_name SET s.offer_id = o.id
WHERE s.offer_id IS NULL AND s.page_id IS NOT NULL;
INSERT INTO catalog_offer_products (offer_id, position, product_type, furniture_id, amount, extra_param)
SELECT s.offer_id, 0, 'furni', s.furniture_id, 1, '' FROM staff_plan_140 s
WHERE s.offer_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM catalog_offer_products op WHERE op.offer_id = s.offer_id);
INSERT IGNORE INTO catalog_page_offers (page_id, offer_id, position)
SELECT s.page_id, s.offer_id,
    (SELECT COALESCE(MAX(po.position), -1) + 1 FROM catalog_page_offers po WHERE po.page_id = s.page_id) + (
        SELECT COUNT(*) FROM staff_plan_140 t WHERE t.page_id = s.page_id AND t.item_name < s.item_name AND NOT EXISTS (
            SELECT 1 FROM catalog_page_offers x WHERE x.page_id = t.page_id AND x.offer_id = t.offer_id))
FROM staff_plan_140 s WHERE s.offer_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM catalog_page_offers x WHERE x.page_id = s.page_id AND x.offer_id = s.offer_id);

DROP TEMPORARY TABLE staff_plan_140;
DROP TEMPORARY TABLE link_plan_140;
DROP TEMPORARY TABLE product_plan_140;
DROP TEMPORARY TABLE offer_plan_140;

COMMIT;
