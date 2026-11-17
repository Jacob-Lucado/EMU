-- Users without their own home room go to this room on login and on "go home". 0 turns it off. Idempotent.
INSERT IGNORE INTO server_settings (`key`, `value`) VALUES ('global_home_room', '0');
