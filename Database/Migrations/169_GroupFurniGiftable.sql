-- Group furni can be picked as a gift in the catalogue, just like Habbo. The gift purchase itself is refused. Idempotent.
UPDATE furniture
SET allow_gift = 1
WHERE interaction_type IN ('gld_item', 'gld_gate', 'guild_forum')
  AND allow_gift = 0;
