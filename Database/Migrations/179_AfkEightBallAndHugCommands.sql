-- Everyone may use :afk, :8ball and :hug.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.afk', 'command', 'Show that you are away from the keyboard.'),
    ('command.8ball', 'command', 'Shake the magic 8 ball in the room.'),
    ('command.hug', 'command', 'Hug another user in the room.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.afk' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.8ball' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.hug' FROM roles WHERE slug = 'default';
