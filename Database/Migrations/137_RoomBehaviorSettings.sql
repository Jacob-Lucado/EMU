-- Door tile, idle sleep, idle autokick and mute-all-pets room settings, with the Habbo defaults for every room. Idempotent.

ALTER TABLE rooms
  ADD COLUMN IF NOT EXISTS leave_on_door_tile TINYINT(1) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS idle_sleep_enabled TINYINT(1) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS idle_sleep_timeout_seconds INT NOT NULL DEFAULT 300,
  ADD COLUMN IF NOT EXISTS idle_autokick_enabled TINYINT(1) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS idle_autokick_timeout_seconds INT NOT NULL DEFAULT 900,
  ADD COLUMN IF NOT EXISTS mute_all_pets TINYINT(1) NOT NULL DEFAULT 0;
