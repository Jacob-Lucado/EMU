ALTER TABLE `rooms` 
	ADD `sale_price` INT(5) NOT NULL DEFAULT '0' AFTER `spush_enabled`, 
	ADD `lay_enabled` ENUM('0','1') NOT NULL DEFAULT '0' AFTER `sale_price`;