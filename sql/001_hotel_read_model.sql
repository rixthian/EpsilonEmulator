CREATE TABLE accounts (
    account_id BIGINT PRIMARY KEY,
    login_name VARCHAR(64) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    last_login_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_accounts_login_name_not_blank CHECK (btrim(login_name) <> ''),
    CONSTRAINT chk_accounts_password_hash_not_blank CHECK (btrim(password_hash) <> ''),
    CONSTRAINT chk_accounts_last_login_after_create CHECK (
        last_login_at_utc IS NULL OR last_login_at_utc >= created_at_utc
    )
);

CREATE TABLE characters (
    character_id BIGINT PRIMARY KEY,
    account_id BIGINT NOT NULL REFERENCES accounts(account_id),
    username VARCHAR(32) NOT NULL UNIQUE,
    motto VARCHAR(128) NOT NULL DEFAULT '',
    figure VARCHAR(255) NOT NULL,
    gender VARCHAR(1) NOT NULL,
    home_room_id BIGINT NOT NULL DEFAULT 0,
    credits_balance INT NOT NULL DEFAULT 0,
    activity_points_balance INT NOT NULL DEFAULT 0,
    respect_points INT NOT NULL DEFAULT 0,
    daily_respect_points INT NOT NULL DEFAULT 3,
    daily_pet_respect_points INT NOT NULL DEFAULT 3,
    CONSTRAINT chk_characters_username_not_blank CHECK (btrim(username) <> ''),
    CONSTRAINT chk_characters_gender CHECK (gender IN ('M', 'F', 'U')),
    CONSTRAINT chk_characters_credits_balance CHECK (credits_balance >= 0),
    CONSTRAINT chk_characters_activity_points_balance CHECK (activity_points_balance >= 0),
    CONSTRAINT chk_characters_respect_points CHECK (respect_points >= 0),
    CONSTRAINT chk_characters_daily_respect_points CHECK (daily_respect_points >= 0),
    CONSTRAINT chk_characters_daily_pet_respect_points CHECK (daily_pet_respect_points >= 0)
);

CREATE TABLE character_subscriptions (
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    subscription_type VARCHAR(16) NOT NULL,
    activated_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (character_id, subscription_type),
    CONSTRAINT chk_character_subscriptions_type_not_blank CHECK (btrim(subscription_type) <> ''),
    CONSTRAINT chk_character_subscriptions_expiry CHECK (expires_at_utc > activated_at_utc)
);

CREATE TABLE room_layouts (
    layout_code VARCHAR(64) PRIMARY KEY,
    door_x INT NOT NULL,
    door_y INT NOT NULL,
    door_z DOUBLE PRECISION NOT NULL,
    door_rotation INT NOT NULL,
    heightmap TEXT NOT NULL,
    public_room_object_sets JSONB NOT NULL DEFAULT '[]'::jsonb,
    club_only BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_room_layouts_code_not_blank CHECK (btrim(layout_code) <> ''),
    CONSTRAINT chk_room_layouts_door_rotation CHECK (door_rotation BETWEEN 0 AND 7),
    CONSTRAINT chk_room_layouts_heightmap_not_blank CHECK (btrim(heightmap) <> ''),
    CONSTRAINT chk_room_layouts_object_sets_array CHECK (jsonb_typeof(public_room_object_sets) = 'array')
);

CREATE TABLE rooms (
    room_id BIGINT PRIMARY KEY,
    room_kind VARCHAR(16) NOT NULL,
    owner_character_id BIGINT NULL REFERENCES characters(character_id),
    name VARCHAR(128) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    category_id INT NOT NULL DEFAULT 0,
    layout_code VARCHAR(64) NOT NULL REFERENCES room_layouts(layout_code),
    access_mode VARCHAR(32) NOT NULL,
    access_password VARCHAR(64) NULL,
    maximum_users INT NOT NULL DEFAULT 25,
    allow_pets BOOLEAN NOT NULL DEFAULT TRUE,
    allow_pet_eating BOOLEAN NOT NULL DEFAULT FALSE,
    allow_walk_through BOOLEAN NOT NULL DEFAULT FALSE,
    hide_walls BOOLEAN NOT NULL DEFAULT FALSE,
    tags JSONB NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT chk_rooms_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT chk_rooms_category_id CHECK (category_id >= 0),
    CONSTRAINT chk_rooms_access_mode CHECK (access_mode IN ('open', 'doorbell', 'password', 'invisible')),
    CONSTRAINT chk_rooms_maximum_users CHECK (maximum_users > 0),
    CONSTRAINT chk_rooms_tags_array CHECK (jsonb_typeof(tags) = 'array')
);

CREATE TABLE item_definitions (
    item_definition_id BIGINT PRIMARY KEY,
    public_name VARCHAR(128) NOT NULL,
    internal_name VARCHAR(128) NOT NULL,
    item_type_code VARCHAR(8) NOT NULL,
    sprite_id INT NOT NULL,
    stack_height DOUBLE PRECISION NOT NULL DEFAULT 0,
    can_stack BOOLEAN NOT NULL DEFAULT TRUE,
    can_sit BOOLEAN NOT NULL DEFAULT FALSE,
    is_walkable BOOLEAN NOT NULL DEFAULT FALSE,
    allow_recycle BOOLEAN NOT NULL DEFAULT TRUE,
    allow_trade BOOLEAN NOT NULL DEFAULT TRUE,
    allow_marketplace_sell BOOLEAN NOT NULL DEFAULT TRUE,
    allow_gift BOOLEAN NOT NULL DEFAULT TRUE,
    allow_inventory_stack BOOLEAN NOT NULL DEFAULT TRUE,
    interaction_type_code VARCHAR(64) NOT NULL,
    interaction_modes_count INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_item_definitions_public_name_not_blank CHECK (btrim(public_name) <> ''),
    CONSTRAINT chk_item_definitions_internal_name_not_blank CHECK (btrim(internal_name) <> ''),
    CONSTRAINT chk_item_definitions_item_type_code_not_blank CHECK (btrim(item_type_code) <> ''),
    CONSTRAINT chk_item_definitions_interaction_type_code_not_blank CHECK (btrim(interaction_type_code) <> ''),
    CONSTRAINT chk_item_definitions_sprite_id CHECK (sprite_id >= 0),
    CONSTRAINT chk_item_definitions_stack_height CHECK (stack_height >= 0),
    CONSTRAINT chk_item_definitions_interaction_modes_count CHECK (interaction_modes_count >= 0)
);

CREATE TABLE room_items (
    item_id BIGINT PRIMARY KEY,
    item_definition_id BIGINT NOT NULL REFERENCES item_definitions(item_definition_id),
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    floor_x INT NULL,
    floor_y INT NULL,
    floor_z DOUBLE PRECISION NULL,
    rotation INT NOT NULL DEFAULT 0,
    wall_position TEXT NULL,
    state_data TEXT NOT NULL DEFAULT '',
    CONSTRAINT chk_room_items_rotation CHECK (rotation >= 0),
    CONSTRAINT chk_room_items_floor_coords_consistent CHECK (
        (floor_x IS NULL AND floor_y IS NULL AND floor_z IS NULL) OR
        (floor_x IS NOT NULL AND floor_y IS NOT NULL AND floor_z IS NOT NULL)
    ),
    CONSTRAINT chk_room_items_floor_or_wall_position CHECK (
        ((floor_x IS NOT NULL AND floor_y IS NOT NULL AND floor_z IS NOT NULL) AND wall_position IS NULL) OR
        ((floor_x IS NULL AND floor_y IS NULL AND floor_z IS NULL) AND wall_position IS NOT NULL)
    )
);

CREATE TABLE pets (
    pet_id BIGINT PRIMARY KEY,
    owner_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    name VARCHAR(64) NOT NULL,
    pet_type_id INT NOT NULL,
    race_code VARCHAR(16) NOT NULL,
    color_code VARCHAR(16) NOT NULL,
    experience INT NOT NULL DEFAULT 0,
    energy INT NOT NULL DEFAULT 100,
    nutrition INT NOT NULL DEFAULT 100,
    respect INT NOT NULL DEFAULT 0,
    level INT NOT NULL DEFAULT 1,
    CONSTRAINT chk_pets_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT chk_pets_pet_type_id CHECK (pet_type_id >= 0),
    CONSTRAINT chk_pets_race_code_not_blank CHECK (btrim(race_code) <> ''),
    CONSTRAINT chk_pets_color_code_not_blank CHECK (btrim(color_code) <> ''),
    CONSTRAINT chk_pets_experience CHECK (experience >= 0),
    CONSTRAINT chk_pets_energy CHECK (energy >= 0),
    CONSTRAINT chk_pets_nutrition CHECK (nutrition >= 0),
    CONSTRAINT chk_pets_respect CHECK (respect >= 0),
    CONSTRAINT chk_pets_level CHECK (level > 0)
);

CREATE INDEX idx_rooms_layout_code ON rooms(layout_code);
CREATE INDEX idx_rooms_owner_character_id ON rooms(owner_character_id);
CREATE INDEX idx_room_items_room_id ON room_items(room_id);
CREATE INDEX idx_room_items_item_definition_id ON room_items(item_definition_id);
CREATE INDEX idx_pets_owner_character_id ON pets(owner_character_id);
CREATE INDEX idx_pets_room_id ON pets(room_id);
CREATE INDEX idx_characters_account_id ON characters(account_id);
CREATE INDEX idx_character_subscriptions_character_id ON character_subscriptions(character_id);
CREATE UNIQUE INDEX idx_accounts_login_name_lower ON accounts (lower(login_name));
CREATE UNIQUE INDEX idx_characters_username_lower ON characters (lower(username));
