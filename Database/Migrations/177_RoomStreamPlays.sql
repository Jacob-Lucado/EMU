-- One row for every song a room starts with :music, for the music charts on the website.
CREATE TABLE IF NOT EXISTS room_stream_plays (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    room_id INT UNSIGNED NOT NULL,
    set_by VARCHAR(50) NOT NULL,
    kind VARCHAR(16) NOT NULL,
    url VARCHAR(500) NOT NULL,
    played_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_room_stream_plays_played_at (played_at),
    KEY idx_room_stream_plays_set_by (set_by)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
