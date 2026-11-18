-- Vending machines that give an avatar effect: the effect id is the machine's effect_id. Only furniture with furnidata is changed. Idempotent.

UPDATE furniture SET effect_id = 164 WHERE has_furnidata = 1 AND item_name = 'pirate_gunrack' AND effect_id <> 164;

UPDATE furniture SET effect_id = 162 WHERE has_furnidata = 1 AND item_name IN ('pirate_swordrack', 'vikings_weapon') AND effect_id <> 162;

UPDATE furniture SET effect_id = 76 WHERE has_furnidata = 1 AND item_name = 'tiki_c15_leigiver' AND effect_id <> 76;
