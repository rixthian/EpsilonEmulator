CREATE TABLE figure_parts (
    figure_part_id BIGINT PRIMARY KEY,
    category_code VARCHAR(32) NOT NULL,
    part_code VARCHAR(64) NOT NULL UNIQUE,
    gender VARCHAR(1) NOT NULL,
    club_level INT NOT NULL DEFAULT 0,
    color_group_code VARCHAR(32) NULL,
    display_order INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_figure_parts_category_not_blank CHECK (btrim(category_code) <> ''),
    CONSTRAINT chk_figure_parts_part_code_not_blank CHECK (btrim(part_code) <> ''),
    CONSTRAINT chk_figure_parts_gender CHECK (gender IN ('M', 'F', 'U')),
    CONSTRAINT chk_figure_parts_club_level CHECK (club_level >= 0),
    CONSTRAINT chk_figure_parts_display_order CHECK (display_order >= 0)
);

CREATE TABLE character_looks (
    character_id BIGINT PRIMARY KEY REFERENCES characters(character_id),
    current_figure_code VARCHAR(255) NOT NULL,
    current_gender VARCHAR(1) NOT NULL,
    last_changed_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_character_looks_figure_not_blank CHECK (btrim(current_figure_code) <> ''),
    CONSTRAINT chk_character_looks_gender CHECK (current_gender IN ('M', 'F', 'U'))
);

CREATE TABLE character_clothing (
    clothing_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    clothing_slot VARCHAR(32) NOT NULL,
    figure_part_id BIGINT NOT NULL REFERENCES figure_parts(figure_part_id),
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    granted_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_character_clothing_slot_not_blank CHECK (btrim(clothing_slot) <> ''),
    CONSTRAINT chk_character_clothing_expiry CHECK (
        expires_at_utc IS NULL OR expires_at_utc > granted_at_utc
    )
);

CREATE TABLE effect_definitions (
    effect_id INT PRIMARY KEY,
    effect_code VARCHAR(64) NOT NULL UNIQUE,
    display_name VARCHAR(128) NOT NULL,
    duration_seconds INT NOT NULL,
    is_temporary BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_effect_definitions_code_not_blank CHECK (btrim(effect_code) <> ''),
    CONSTRAINT chk_effect_definitions_display_name_not_blank CHECK (btrim(display_name) <> ''),
    CONSTRAINT chk_effect_definitions_duration_seconds CHECK (duration_seconds > 0)
);

CREATE TABLE character_effects (
    character_effect_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    effect_id INT NOT NULL REFERENCES effect_definitions(effect_id),
    granted_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NULL,
    remaining_seconds INT NOT NULL DEFAULT 0,
    is_equipped BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_character_effects_remaining_seconds CHECK (remaining_seconds >= 0),
    CONSTRAINT chk_character_effects_expiry CHECK (
        expires_at_utc IS NULL OR expires_at_utc > granted_at_utc
    )
);

CREATE TABLE badge_definitions (
    badge_code VARCHAR(64) PRIMARY KEY,
    badge_name VARCHAR(128) NOT NULL,
    badge_group VARCHAR(64) NULL,
    required_right VARCHAR(128) NULL,
    asset_path VARCHAR(255) NOT NULL,
    asset_kind VARCHAR(16) NOT NULL DEFAULT 'gif',
    CONSTRAINT chk_badge_definitions_code_not_blank CHECK (btrim(badge_code) <> ''),
    CONSTRAINT chk_badge_definitions_name_not_blank CHECK (btrim(badge_name) <> ''),
    CONSTRAINT chk_badge_definitions_asset_path_not_blank CHECK (btrim(asset_path) <> ''),
    CONSTRAINT chk_badge_definitions_asset_kind CHECK (asset_kind IN ('gif', 'png', 'webp', 'svg'))
);

CREATE TABLE character_badges (
    character_badge_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    badge_code VARCHAR(64) NOT NULL REFERENCES badge_definitions(badge_code),
    slot_index INT NOT NULL DEFAULT 0,
    is_selected BOOLEAN NOT NULL DEFAULT FALSE,
    granted_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_character_badges_slot_index CHECK (slot_index >= 0)
);

CREATE TABLE achievement_definitions (
    achievement_code VARCHAR(64) PRIMARY KEY,
    category_code VARCHAR(32) NOT NULL,
    max_level INT NOT NULL,
    point_value INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_achievement_definitions_code_not_blank CHECK (btrim(achievement_code) <> ''),
    CONSTRAINT chk_achievement_definitions_category_not_blank CHECK (btrim(category_code) <> ''),
    CONSTRAINT chk_achievement_definitions_max_level CHECK (max_level > 0),
    CONSTRAINT chk_achievement_definitions_point_value CHECK (point_value >= 0)
);

CREATE TABLE character_achievements (
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    achievement_code VARCHAR(64) NOT NULL REFERENCES achievement_definitions(achievement_code),
    current_level INT NOT NULL DEFAULT 0,
    progress_value BIGINT NOT NULL DEFAULT 0,
    completed_at_utc TIMESTAMPTZ NULL,
    PRIMARY KEY (character_id, achievement_code),
    CONSTRAINT chk_character_achievements_current_level CHECK (current_level >= 0),
    CONSTRAINT chk_character_achievements_progress_value CHECK (progress_value >= 0)
);

CREATE INDEX idx_character_clothing_character_id ON character_clothing(character_id);
CREATE INDEX idx_character_effects_character_id ON character_effects(character_id);
CREATE INDEX idx_character_badges_character_id ON character_badges(character_id);
