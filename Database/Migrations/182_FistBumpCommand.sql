-- Everyone may use :fistbump.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.fistbump', 'command', 'Fist bump another user in the room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.fistbump' FROM roles WHERE slug = 'default';
