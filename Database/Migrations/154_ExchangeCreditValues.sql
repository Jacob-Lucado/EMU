-- A credit furni pays the credits in its furni asset logic, and Habbo names it after that value: CF_<credits>_<name>
-- or CF_diamond_<credits>. Items that had no value could not be redeemed. Idempotent.
UPDATE furniture SET interaction_type = 'exchange' WHERE item_name = 'CF_250_stagmount' AND interaction_type = 'default';

UPDATE furniture
SET behaviour_data = CAST(SUBSTRING_INDEX(IF(item_name LIKE 'CF\_diamond\_%', item_name, SUBSTRING_INDEX(item_name, '_', 2)), '_', -1) AS UNSIGNED)
WHERE interaction_type = 'exchange' AND behaviour_data <= 0
  AND (item_name REGEXP '^CF_[0-9]+_' OR item_name REGEXP '^CF_diamond_[0-9]+$');
