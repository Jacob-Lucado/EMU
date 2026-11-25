-- Everyone may use :steal.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.steal', 'command', 'Try to snatch the hand item of a user next to you.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.steal' FROM roles WHERE slug = 'default';
