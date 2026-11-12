ALTER TABLE `user_ignores` DROP `id`;
ALTER TABLE `user_ignores` ADD PRIMARY KEY(`user_id`, `ignore_id`);