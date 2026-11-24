-- Everyone may use :pick.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.pick', 'command', 'Let the room pick one of several options.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.pick' FROM roles WHERE slug = 'default';
