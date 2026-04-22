CREATE TABLE wallet_accounts (
    character_id BIGINT PRIMARY KEY REFERENCES characters(character_id),
    credits_balance INT NOT NULL DEFAULT 0,
    duckets_balance INT NOT NULL DEFAULT 0,
    snow_balance INT NOT NULL DEFAULT 0,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_wallet_accounts_credits_balance CHECK (credits_balance >= 0),
    CONSTRAINT chk_wallet_accounts_duckets_balance CHECK (duckets_balance >= 0),
    CONSTRAINT chk_wallet_accounts_snow_balance CHECK (snow_balance >= 0)
);

CREATE TABLE wallet_ledger (
    ledger_entry_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    currency_code VARCHAR(16) NOT NULL,
    amount_delta INT NOT NULL,
    reason_code VARCHAR(64) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    correlation_key VARCHAR(128) NULL,
    CONSTRAINT chk_wallet_ledger_currency_code_not_blank CHECK (btrim(currency_code) <> ''),
    CONSTRAINT chk_wallet_ledger_reason_code_not_blank CHECK (btrim(reason_code) <> '')
);

CREATE TABLE item_definitions (
    item_definition_id BIGINT PRIMARY KEY,
    internal_name VARCHAR(128) NOT NULL UNIQUE,
    public_name VARCHAR(128) NOT NULL,
    item_type_code VARCHAR(8) NOT NULL,
    interaction_type_code VARCHAR(64) NOT NULL,
    sprite_id INT NOT NULL,
    allow_trade BOOLEAN NOT NULL DEFAULT TRUE,
    allow_gift BOOLEAN NOT NULL DEFAULT TRUE,
    allow_recycle BOOLEAN NOT NULL DEFAULT TRUE,
    allow_inventory_stack BOOLEAN NOT NULL DEFAULT TRUE,
    can_walk_on BOOLEAN NOT NULL DEFAULT FALSE,
    can_sit_on BOOLEAN NOT NULL DEFAULT FALSE,
    stack_height DOUBLE PRECISION NOT NULL DEFAULT 0,
    interaction_modes_count INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_item_definitions_internal_name_not_blank CHECK (btrim(internal_name) <> ''),
    CONSTRAINT chk_item_definitions_public_name_not_blank CHECK (btrim(public_name) <> ''),
    CONSTRAINT chk_item_definitions_item_type_code_not_blank CHECK (btrim(item_type_code) <> ''),
    CONSTRAINT chk_item_definitions_interaction_type_code_not_blank CHECK (btrim(interaction_type_code) <> ''),
    CONSTRAINT chk_item_definitions_sprite_id CHECK (sprite_id >= 0),
    CONSTRAINT chk_item_definitions_stack_height CHECK (stack_height >= 0),
    CONSTRAINT chk_item_definitions_interaction_modes_count CHECK (interaction_modes_count >= 0)
);

CREATE TABLE item_instances (
    item_id BIGINT PRIMARY KEY,
    item_definition_id BIGINT NOT NULL REFERENCES item_definitions(item_definition_id),
    owner_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    current_room_id BIGINT NULL REFERENCES rooms(room_id),
    floor_x INT NULL,
    floor_y INT NULL,
    floor_z DOUBLE PRECISION NULL,
    wall_position TEXT NULL,
    rotation INT NOT NULL DEFAULT 0,
    state_data TEXT NOT NULL DEFAULT '',
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_item_instances_rotation CHECK (rotation >= 0),
    CONSTRAINT chk_item_instances_floor_coords_consistent CHECK (
        (floor_x IS NULL AND floor_y IS NULL AND floor_z IS NULL) OR
        (floor_x IS NOT NULL AND floor_y IS NOT NULL AND floor_z IS NOT NULL)
    )
);

CREATE TABLE catalog_pages (
    catalog_page_id BIGINT PRIMARY KEY,
    page_key VARCHAR(64) NOT NULL UNIQUE,
    parent_page_id BIGINT NULL REFERENCES catalog_pages(catalog_page_id),
    caption VARCHAR(128) NOT NULL,
    layout_code VARCHAR(64) NOT NULL,
    page_kind VARCHAR(16) NOT NULL DEFAULT 'normal',
    minimum_rank INT NOT NULL DEFAULT 0,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    is_visible BOOLEAN NOT NULL DEFAULT TRUE,
    display_order INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_catalog_pages_page_key_not_blank CHECK (btrim(page_key) <> ''),
    CONSTRAINT chk_catalog_pages_caption_not_blank CHECK (btrim(caption) <> ''),
    CONSTRAINT chk_catalog_pages_layout_code_not_blank CHECK (btrim(layout_code) <> ''),
    CONSTRAINT chk_catalog_pages_kind CHECK (page_kind IN ('normal', 'frontpage', 'bundle', 'seasonal')),
    CONSTRAINT chk_catalog_pages_minimum_rank CHECK (minimum_rank >= 0),
    CONSTRAINT chk_catalog_pages_display_order CHECK (display_order >= 0)
);

CREATE TABLE catalog_offers (
    catalog_offer_id BIGINT PRIMARY KEY,
    catalog_page_id BIGINT NOT NULL REFERENCES catalog_pages(catalog_page_id),
    offer_key VARCHAR(64) NOT NULL UNIQUE,
    offer_name VARCHAR(128) NOT NULL,
    offer_kind VARCHAR(16) NOT NULL DEFAULT 'single',
    credits_cost INT NOT NULL DEFAULT 0,
    duckets_cost INT NOT NULL DEFAULT 0,
    snow_cost INT NOT NULL DEFAULT 0,
    is_limited BOOLEAN NOT NULL DEFAULT FALSE,
    total_limited_stock INT NULL,
    remaining_limited_stock INT NULL,
    CONSTRAINT chk_catalog_offers_offer_key_not_blank CHECK (btrim(offer_key) <> ''),
    CONSTRAINT chk_catalog_offers_offer_name_not_blank CHECK (btrim(offer_name) <> ''),
    CONSTRAINT chk_catalog_offers_kind CHECK (offer_kind IN ('single', 'bundle', 'collectible', 'effect', 'subscription')),
    CONSTRAINT chk_catalog_offers_credits_cost CHECK (credits_cost >= 0),
    CONSTRAINT chk_catalog_offers_duckets_cost CHECK (duckets_cost >= 0),
    CONSTRAINT chk_catalog_offers_snow_cost CHECK (snow_cost >= 0),
    CONSTRAINT chk_catalog_offers_stock_total CHECK (total_limited_stock IS NULL OR total_limited_stock >= 0),
    CONSTRAINT chk_catalog_offers_stock_remaining CHECK (remaining_limited_stock IS NULL OR remaining_limited_stock >= 0)
);

CREATE TABLE catalog_products (
    catalog_offer_id BIGINT NOT NULL REFERENCES catalog_offers(catalog_offer_id),
    line_number INT NOT NULL,
    item_definition_id BIGINT NOT NULL REFERENCES item_definitions(item_definition_id),
    product_amount INT NOT NULL DEFAULT 1,
    extra_data TEXT NULL,
    PRIMARY KEY (catalog_offer_id, line_number),
    CONSTRAINT chk_catalog_products_line_number CHECK (line_number >= 0),
    CONSTRAINT chk_catalog_products_amount CHECK (product_amount > 0)
);

CREATE TABLE vouchers (
    voucher_code VARCHAR(64) PRIMARY KEY,
    display_name VARCHAR(128) NOT NULL,
    reward_currency_code VARCHAR(16) NOT NULL,
    reward_amount INT NOT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    starts_at_utc TIMESTAMPTZ NULL,
    ends_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_vouchers_code_not_blank CHECK (btrim(voucher_code) <> ''),
    CONSTRAINT chk_vouchers_display_name_not_blank CHECK (btrim(display_name) <> ''),
    CONSTRAINT chk_vouchers_currency_not_blank CHECK (btrim(reward_currency_code) <> ''),
    CONSTRAINT chk_vouchers_reward_amount CHECK (reward_amount > 0),
    CONSTRAINT chk_vouchers_window CHECK (
        starts_at_utc IS NULL OR ends_at_utc IS NULL OR ends_at_utc > starts_at_utc
    )
);

CREATE TABLE voucher_redemptions (
    voucher_redemption_id BIGINT PRIMARY KEY,
    voucher_code VARCHAR(64) NOT NULL REFERENCES vouchers(voucher_code),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    redeemed_at_utc TIMESTAMPTZ NOT NULL,
    UNIQUE (voucher_code, character_id)
);

CREATE TABLE collectible_definitions (
    collectible_id BIGINT PRIMARY KEY,
    collectible_key VARCHAR(64) NOT NULL UNIQUE,
    offer_id BIGINT NULL REFERENCES catalog_offers(catalog_offer_id),
    release_week_code VARCHAR(16) NULL,
    rarity_tier VARCHAR(16) NOT NULL DEFAULT 'standard',
    CONSTRAINT chk_collectible_definitions_key_not_blank CHECK (btrim(collectible_key) <> ''),
    CONSTRAINT chk_collectible_definitions_rarity CHECK (rarity_tier IN ('standard', 'rare', 'super_rare', 'limited'))
);

CREATE TABLE ecotron_rewards (
    ecotron_reward_id BIGINT PRIMARY KEY,
    reward_key VARCHAR(64) NOT NULL UNIQUE,
    item_definition_id BIGINT NOT NULL REFERENCES item_definitions(item_definition_id),
    weight_value INT NOT NULL DEFAULT 1,
    CONSTRAINT chk_ecotron_rewards_key_not_blank CHECK (btrim(reward_key) <> ''),
    CONSTRAINT chk_ecotron_rewards_weight_value CHECK (weight_value > 0)
);

CREATE INDEX idx_wallet_ledger_character_id ON wallet_ledger(character_id, created_at_utc DESC);
CREATE INDEX idx_item_instances_owner_character_id ON item_instances(owner_character_id);
CREATE INDEX idx_item_instances_current_room_id ON item_instances(current_room_id);
CREATE INDEX idx_catalog_pages_parent_page_id ON catalog_pages(parent_page_id);
CREATE INDEX idx_catalog_offers_catalog_page_id ON catalog_offers(catalog_page_id);
