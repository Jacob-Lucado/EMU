-- Everyone may use :tv; the command itself checks for room rights.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES ('command.tv', 'command', 'Play a YouTube video on the TVs in a room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.tv' FROM roles WHERE slug = 'default';
