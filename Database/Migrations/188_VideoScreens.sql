-- What each Video Screen shows: "" for nothing, "@music" for the room music, or a YouTube video id. Everyone with rights in a room may use :screen.
CREATE TABLE IF NOT EXISTS room_video_screens (
    item_id INT UNSIGNED NOT NULL,
    value VARCHAR(16) NOT NULL DEFAULT '@music',
    PRIMARY KEY (item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.screen', 'command', 'Choose what a Video Screen shows.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.screen' FROM roles WHERE slug = 'default';
