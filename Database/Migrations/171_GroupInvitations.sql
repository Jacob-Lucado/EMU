-- Pending group invitations. An invitation is the owner's consent, so accepting it makes the user a member of any group type.
-- One invitation per group and user; a new invitation refreshes the inviter, room and time. Idempotent.

CREATE TABLE IF NOT EXISTS `group_invitations` (
  `group_id` int(11) unsigned NOT NULL,
  `user_id` int(11) NOT NULL,
  `inviter_id` int(11) NOT NULL,
  `room_id` int(10) unsigned NOT NULL DEFAULT 0,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`group_id`,`user_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `fk_group_invitations_group_id` FOREIGN KEY (`group_id`) REFERENCES `groups` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_group_invitations_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
