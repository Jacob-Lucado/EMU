ALTER TABLE `messenger_friendships` DROP `id`;
ALTER TABLE `messenger_friendships` ADD PRIMARY KEY(`user_one_id`, `user_two_id`);
INSERT INTO `messenger_friendships` (`user_one_id`, `user_two_id`) SELECT `user_two_id`, `user_one_id` FROM `messenger_friendships`;
ALTER TABLE `messenger_friendships` ADD `relationship` INT(1) NOT NULL DEFAULT '0' AFTER `user_two_id`;
UPDATE `messenger_friendships` INNER JOIN `user_relationships` ON `messenger_friendships`.`user_one_id` = `user_relationships`.`user_id` AND `messenger_friendships`.`user_two_id` = `user_relationships`.`target` SET `messenger_friendships`.`relationship` = `type`;
DROP TABLE ` user_relationships `;