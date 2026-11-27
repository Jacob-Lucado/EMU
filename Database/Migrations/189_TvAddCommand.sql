INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES ('command.tvadd', 'command', 'Add a YouTube video to the TV playlist.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.tvadd' FROM roles WHERE slug = 'default';
