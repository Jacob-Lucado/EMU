-- The stock catalog front page promotions stay up until the end of 2030. Promotions a hotel added or pointed at
-- another page keep their end date.
UPDATE `catalog_promotions` SET `expires_at` = '2030-12-31 23:59:59'
WHERE (`id`, `page_link`) IN ((6, 'house20bundd'), (7, 'hween26furni'), (8, 'ducket_furni'))
    AND `expires_at` < '2030-12-31 23:59:59';
