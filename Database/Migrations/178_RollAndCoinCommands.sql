-- Everyone may use :roll and :coin.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.roll', 'command', 'Roll a dice in the room.'),
    ('command.coin', 'command', 'Flip a coin in the room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.roll' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.coin' FROM roles WHERE slug = 'default';
