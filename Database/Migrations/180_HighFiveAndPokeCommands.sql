-- Everyone may use :highfive and :poke.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.highfive', 'command', 'Give another user in the room a high five.'),
    ('command.poke', 'command', 'Poke another user in the room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.highfive' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.poke' FROM roles WHERE slug = 'default';
