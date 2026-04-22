CREATE TABLE sound_sets (
    sound_set_id BIGINT PRIMARY KEY,
    sound_key VARCHAR(64) NOT NULL UNIQUE,
    display_name VARCHAR(128) NOT NULL,
    asset_path VARCHAR(255) NOT NULL,
    duration_ms INT NOT NULL,
    CONSTRAINT chk_sound_sets_key_not_blank CHECK (btrim(sound_key) <> ''),
    CONSTRAINT chk_sound_sets_display_name_not_blank CHECK (btrim(display_name) <> ''),
    CONSTRAINT chk_sound_sets_asset_path_not_blank CHECK (btrim(asset_path) <> ''),
    CONSTRAINT chk_sound_sets_duration_ms CHECK (duration_ms > 0)
);

CREATE TABLE trax_song_definitions (
    trax_song_id BIGINT PRIMARY KEY,
    song_name VARCHAR(128) NOT NULL,
    sound_set_id BIGINT NOT NULL REFERENCES sound_sets(sound_set_id),
    track_data_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_trax_song_definitions_name_not_blank CHECK (btrim(song_name) <> ''),
    CONSTRAINT chk_trax_song_definitions_track_data_array CHECK (jsonb_typeof(track_data_json) = 'array')
);

CREATE TABLE song_disks (
    disk_item_id BIGINT PRIMARY KEY,
    owner_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    trax_song_id BIGINT NOT NULL REFERENCES trax_song_definitions(trax_song_id),
    created_at_utc TIMESTAMPTZ NOT NULL
);

CREATE TABLE jukebox_queues (
    queue_entry_id BIGINT PRIMARY KEY,
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    disk_item_id BIGINT NOT NULL REFERENCES song_disks(disk_item_id),
    queue_position INT NOT NULL,
    queued_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_jukebox_queues_queue_position CHECK (queue_position >= 0)
);

CREATE TABLE room_audio_profiles (
    room_id BIGINT PRIMARY KEY REFERENCES rooms(room_id),
    ambient_sound_set_id BIGINT NULL REFERENCES sound_sets(sound_set_id),
    jukebox_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    trax_machine_enabled BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_song_disks_owner_character_id ON song_disks(owner_character_id);
CREATE UNIQUE INDEX idx_jukebox_queues_room_id_queue_position ON jukebox_queues(room_id, queue_position);
