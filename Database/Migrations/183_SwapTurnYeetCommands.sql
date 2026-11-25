-- Everyone may use :swap, :turn and :yeet.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.swap', 'command', 'Swap places with a user standing next to you.'),
    ('command.turn', 'command', 'Turn a user next to you around to face away from you.'),
    ('command.yeet', 'command', 'Throw a user next to you three tiles away.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.swap' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.turn' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.yeet' FROM roles WHERE slug = 'default';
