-- Everyone may use :treat.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.treat', 'command', 'Hand a user next to you a drink or a snack.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.treat' FROM roles WHERE slug = 'default';
