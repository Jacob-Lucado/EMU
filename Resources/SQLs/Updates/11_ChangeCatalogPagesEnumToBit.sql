ALTER TABLE `catalog_pages` DROP `visible`;
ALTER TABLE `catalog_pages` ADD `visible` bit(1) NOT NULL DEFAULT b'1';
ALTER TABLE `catalog_pages` DROP `enabled`;
ALTER TABLE `catalog_pages` ADD `enabled` bit(1) NOT NULL DEFAULT b'1';