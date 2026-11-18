-- Vending machines hand out the hand items Habbo's machines hand out. Only furniture with furnidata is changed. Idempotent.

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '18' WHERE has_furnidata = 1 AND item_name IN ('bathroom_sinkmodule1', 'bathroom_sinkmodule2', 'bathroom_sinkmodule3', 'bathroom_sinkmodule4') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '18');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '19' WHERE has_furnidata = 1 AND item_name IN ('bazaar_c17_bubblejuiceblower') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '19');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '34' WHERE has_furnidata = 1 AND item_name IN ('ktchn_inspctr', 'pirate_barrel3') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '34');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '38' WHERE has_furnidata = 1 AND item_name IN ('pirate_barrel2') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '38');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '42' WHERE has_furnidata = 1 AND item_name IN ('skorea_c22_drinksmachine') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '42');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '87' WHERE has_furnidata = 1 AND item_name IN ('wildwest_pump') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '87');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '88' WHERE has_furnidata = 1 AND item_name IN ('wildwest_cabinet') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '88');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '1060' WHERE has_furnidata = 1 AND item_name IN ('sb_cans') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '1060');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '1078' WHERE has_furnidata = 1 AND item_name IN ('bazaar_c17_sticklizard') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '1078');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '120,121' WHERE has_furnidata = 1 AND item_name IN ('tokyo_c18_snackdisplay2') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '120,121');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '32,35,40' WHERE has_furnidata = 1 AND item_name IN ('bling11_slot') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '32,35,40');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '67,68,69' WHERE has_furnidata = 1 AND item_name IN ('diner_gumvendor*10') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '67,68,69');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '150,151,152' WHERE has_furnidata = 1 AND item_name IN ('skorea_c22_dalgona') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '150,151,152');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '29,50,74,142' WHERE has_furnidata = 1 AND item_name IN ('mafia_c26_drinkcabinet') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '29,50,74,142');

UPDATE furniture SET interaction_type = 'vendingmachine', vending_ids = '24,29,30,35,53,62,87,90,1041' WHERE has_furnidata = 1 AND item_name IN ('cpunk_c15_bar') AND (interaction_type <> 'vendingmachine' OR vending_ids <> '24,29,30,35,53,62,87,90,1041');

UPDATE furniture SET interaction_type = 'vendingmachine' WHERE has_furnidata = 1 AND item_name IN ('lm_jackbox', 'pirate_gunrack', 'pirate_swordrack', 'tiki_c15_leigiver', 'vikings_weapon') AND interaction_type <> 'vendingmachine';
