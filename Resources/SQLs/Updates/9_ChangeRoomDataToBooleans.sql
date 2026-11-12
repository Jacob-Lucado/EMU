ALTER TABLE `rooms` CHANGE `allow_pets` `allow_pets` BOOLEAN NOT NULL DEFAULT FALSE, 
CHANGE `allow_pets_eat` `allow_pets_eat` BOOLEAN NOT NULL DEFAULT FALSE, 
CHANGE `room_blocking_disabled` `room_blocking_disabled` BOOLEAN NOT NULL DEFAULT FALSE, 
CHANGE `allow_hidewall` `allow_hidewall` BOOLEAN NOT NULL DEFAULT FALSE; 