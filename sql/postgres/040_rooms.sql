CREATE TABLE room_layouts (
    layout_code VARCHAR(64) PRIMARY KEY,
    layout_kind VARCHAR(16) NOT NULL DEFAULT 'private',
    door_x INT NOT NULL,
    door_y INT NOT NULL,
    door_z DOUBLE PRECISION NOT NULL,
    door_rotation INT NOT NULL,
    heightmap TEXT NOT NULL,
    public_object_sets JSONB NOT NULL DEFAULT '[]'::jsonb,
    club_only BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_room_layouts_code_not_blank CHECK (btrim(layout_code) <> ''),
    CONSTRAINT chk_room_layouts_kind CHECK (layout_kind IN ('private', 'public', 'game')),
    CONSTRAINT chk_room_layouts_door_rotation CHECK (door_rotation BETWEEN 0 AND 7),
    CONSTRAINT chk_room_layouts_heightmap_not_blank CHECK (btrim(heightmap) <> ''),
    CONSTRAINT chk_room_layouts_public_object_sets_array CHECK (jsonb_typeof(public_object_sets) = 'array')
);

CREATE TABLE rooms (
    room_id BIGINT PRIMARY KEY,
    room_kind VARCHAR(16) NOT NULL,
    owner_character_id BIGINT NULL REFERENCES characters(character_id),
    layout_code VARCHAR(64) NOT NULL REFERENCES room_layouts(layout_code),
    room_name VARCHAR(128) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    category_id INT NOT NULL DEFAULT 0,
    access_mode VARCHAR(32) NOT NULL DEFAULT 'open',
    access_password VARCHAR(64) NULL,
    maximum_users INT NOT NULL DEFAULT 25,
    score_value INT NOT NULL DEFAULT 0,
    allow_pets BOOLEAN NOT NULL DEFAULT TRUE,
    allow_walk_through BOOLEAN NOT NULL DEFAULT FALSE,
    hide_walls BOOLEAN NOT NULL DEFAULT FALSE,
    tags JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_rooms_kind CHECK (room_kind IN ('private', 'public', 'game')),
    CONSTRAINT chk_rooms_name_not_blank CHECK (btrim(room_name) <> ''),
    CONSTRAINT chk_rooms_category_id CHECK (category_id >= 0),
    CONSTRAINT chk_rooms_access_mode CHECK (access_mode IN ('open', 'doorbell', 'password', 'invisible')),
    CONSTRAINT chk_rooms_maximum_users CHECK (maximum_users > 0),
    CONSTRAINT chk_rooms_score_value CHECK (score_value >= 0),
    CONSTRAINT chk_rooms_tags_array CHECK (jsonb_typeof(tags) = 'array')
);

CREATE TABLE room_rights (
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    right_level VARCHAR(16) NOT NULL DEFAULT 'controller',
    granted_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (room_id, character_id),
    CONSTRAINT chk_room_rights_level CHECK (right_level IN ('controller', 'co_owner'))
);

CREATE TABLE room_ratings (
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    rating_value SMALLINT NOT NULL,
    expires_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (room_id, character_id),
    CONSTRAINT chk_room_ratings_value CHECK (rating_value IN (-1, 1))
);

CREATE TABLE public_room_entries (
    public_room_entry_id INT PRIMARY KEY,
    room_id BIGINT NOT NULL UNIQUE REFERENCES rooms(room_id),
    entry_code VARCHAR(64) NOT NULL UNIQUE,
    caption VARCHAR(128) NOT NULL,
    navigator_category VARCHAR(32) NOT NULL,
    package_key VARCHAR(64) NOT NULL,
    is_recommended BOOLEAN NOT NULL DEFAULT FALSE,
    display_order INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_public_room_entries_code_not_blank CHECK (btrim(entry_code) <> ''),
    CONSTRAINT chk_public_room_entries_caption_not_blank CHECK (btrim(caption) <> ''),
    CONSTRAINT chk_public_room_entries_navigator_category_not_blank CHECK (btrim(navigator_category) <> ''),
    CONSTRAINT chk_public_room_entries_package_key_not_blank CHECK (btrim(package_key) <> ''),
    CONSTRAINT chk_public_room_entries_display_order CHECK (display_order >= 0)
);

CREATE TABLE room_models (
    model_id BIGINT PRIMARY KEY,
    room_id BIGINT NOT NULL UNIQUE REFERENCES rooms(room_id),
    wall_thickness INT NOT NULL DEFAULT 0,
    floor_thickness INT NOT NULL DEFAULT 0,
    ambient_preset VARCHAR(32) NOT NULL DEFAULT 'default',
    effect_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT chk_room_models_effect_json_object CHECK (jsonb_typeof(effect_json) = 'object')
);

CREATE INDEX idx_rooms_owner_character_id ON rooms(owner_character_id);
CREATE INDEX idx_rooms_layout_code ON rooms(layout_code);
CREATE INDEX idx_public_room_entries_category ON public_room_entries(navigator_category, display_order);
