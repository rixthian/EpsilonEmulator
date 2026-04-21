CREATE TABLE accounts (
    account_id BIGINT PRIMARY KEY,
    login_name VARCHAR(64) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    last_login_at_utc TIMESTAMPTZ NULL
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
    daily_pet_respect_points INT NOT NULL DEFAULT 3
);

CREATE TABLE character_subscriptions (
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    subscription_type VARCHAR(16) NOT NULL,
    activated_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (character_id, subscription_type)
);

CREATE TABLE room_layouts (
    layout_code VARCHAR(64) PRIMARY KEY,
    door_x INT NOT NULL,
    door_y INT NOT NULL,
    door_z DOUBLE PRECISION NOT NULL,
    door_rotation INT NOT NULL,
    heightmap TEXT NOT NULL,
    public_room_object_sets JSONB NOT NULL DEFAULT '[]'::jsonb,
    club_only BOOLEAN NOT NULL DEFAULT FALSE
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
    tags JSONB NOT NULL DEFAULT '[]'::jsonb
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
    interaction_modes_count INT NOT NULL DEFAULT 0
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
    state_data TEXT NOT NULL DEFAULT ''
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
    level INT NOT NULL DEFAULT 1
);

CREATE INDEX idx_rooms_layout_code ON rooms(layout_code);
CREATE INDEX idx_room_items_room_id ON room_items(room_id);
CREATE INDEX idx_pets_owner_character_id ON pets(owner_character_id);
CREATE INDEX idx_characters_account_id ON characters(account_id);

