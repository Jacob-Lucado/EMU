-- Group furni start in state 0, just like Habbo. Older purchases stored the group id as the state, so those rows go back to 0. Idempotent.
UPDATE items i
JOIN items_groups g ON g.id = i.id
JOIN furniture f ON f.id = i.base_item
SET i.extra_data = '0'
WHERE f.interaction_type IN ('gld_item', 'gld_gate', 'guild_forum')
  AND i.extra_data REGEXP '^[1-9][0-9]*$'
  AND CAST(i.extra_data AS UNSIGNED) = g.group_id;
