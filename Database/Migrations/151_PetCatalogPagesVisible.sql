-- Shows the bunny and chick pet pages in the catalogue, as Habbo does. Idempotent.
UPDATE catalog_pages SET visible = 1
WHERE catalog_mode = 'NORMAL' AND layout = 'pets' AND link IN ('pet_bunnypink', 'pet_bunnybrown', 'bunny', 'pet_chick');
