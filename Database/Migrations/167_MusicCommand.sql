-- Everyone may use :music; the command itself checks for room rights.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES ('command.music', 'command', 'Play a web audio stream or SoundCloud link in a room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.music' FROM roles WHERE slug = 'default';
