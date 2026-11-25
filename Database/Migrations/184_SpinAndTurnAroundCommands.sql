-- Everyone may use :spin and :turnaround.

INSERT IGNORE INTO acl_permissions (`key`, category, description) VALUES
    ('command.spin', 'command', 'Turn a quarter turn to the right.'),
    ('command.turnaround', 'command', 'Turn around to face the other way.');

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.spin' FROM roles WHERE slug = 'default';

INSERT IGNORE INTO role_permissions (role_id, permission_key)
SELECT id, 'command.turnaround' FROM roles WHERE slug = 'default';
