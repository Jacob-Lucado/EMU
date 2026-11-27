-- Moderation for :music. Staff ban links with :musicmod, and a user who plays banned links 3 times loses :music until the strikes are lifted.
CREATE TABLE IF NOT EXISTS room_stream_bans (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ban_key VARCHAR(200) NOT NULL,
    url VARCHAR(500) NOT NULL,
    reason VARCHAR(200) NOT NULL DEFAULT '',
    banned_by VARCHAR(50) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_room_stream_bans_key (ban_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS room_stream_strikes (
    user_id INT NOT NULL,
    strikes INT UNSIGNED NOT NULL DEFAULT 0,
    restricted TINYINT(1) NOT NULL DEFAULT 0,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.musicmod', 'command', 'Ban or unban songs for :music, and check or lift music strikes.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.musicmod' FROM roles
WHERE slug IN ('trial_moderator', 'moderator', 'senior_moderator', 'administrator', 'manager', 'community_leader', 'developer', 'owner');
