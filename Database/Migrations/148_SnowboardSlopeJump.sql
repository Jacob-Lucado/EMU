-- Give the official snowboard slope and jump their interactions; keep custom definitions and all IDs.
UPDATE furniture SET interaction_type = 'snowb_slope'
WHERE item_name = 'snowb_slope' AND sprite_id = 4390 AND interaction_type = 'default';

UPDATE furniture SET interaction_type = 'snowb_jump'
WHERE item_name = 'snowb_jump' AND sprite_id = 4391 AND interaction_type = 'default';
