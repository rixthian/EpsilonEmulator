CREATE TABLE staff_roles (
    staff_role_id BIGINT PRIMARY KEY,
    role_key VARCHAR(32) NOT NULL UNIQUE,
    display_name VARCHAR(64) NOT NULL,
    hierarchy_level INT NOT NULL,
    is_staff_role BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_staff_roles_key_not_blank CHECK (btrim(role_key) <> ''),
    CONSTRAINT chk_staff_roles_display_name_not_blank CHECK (btrim(display_name) <> '')
);

CREATE TABLE capabilities (
    capability_key VARCHAR(64) PRIMARY KEY,
    capability_group VARCHAR(32) NOT NULL,
    description_text VARCHAR(255) NOT NULL,
    CONSTRAINT chk_capabilities_key_not_blank CHECK (btrim(capability_key) <> ''),
    CONSTRAINT chk_capabilities_group_not_blank CHECK (btrim(capability_group) <> ''),
    CONSTRAINT chk_capabilities_description_not_blank CHECK (btrim(description_text) <> '')
);

CREATE TABLE role_capabilities (
    staff_role_id BIGINT NOT NULL REFERENCES staff_roles(staff_role_id),
    capability_key VARCHAR(64) NOT NULL REFERENCES capabilities(capability_key),
    PRIMARY KEY (staff_role_id, capability_key)
);

CREATE TABLE character_roles (
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    staff_role_id BIGINT NOT NULL REFERENCES staff_roles(staff_role_id),
    assigned_at_utc TIMESTAMPTZ NOT NULL,
    assigned_by_character_id BIGINT NULL REFERENCES characters(character_id),
    PRIMARY KEY (character_id, staff_role_id)
);

CREATE TABLE support_calls (
    support_call_id BIGINT PRIMARY KEY,
    room_id BIGINT NULL REFERENCES rooms(room_id),
    caller_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    handler_character_id BIGINT NULL REFERENCES characters(character_id),
    call_state VARCHAR(16) NOT NULL DEFAULT 'open',
    subject_text VARCHAR(128) NOT NULL,
    body_text TEXT NOT NULL DEFAULT '',
    opened_at_utc TIMESTAMPTZ NOT NULL,
    closed_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_support_calls_state CHECK (call_state IN ('open', 'claimed', 'closed', 'dismissed')),
    CONSTRAINT chk_support_calls_subject_not_blank CHECK (btrim(subject_text) <> ''),
    CONSTRAINT chk_support_calls_closed_after_open CHECK (
        closed_at_utc IS NULL OR closed_at_utc >= opened_at_utc
    )
);

CREATE TABLE moderation_actions (
    moderation_action_id BIGINT PRIMARY KEY,
    moderator_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    target_character_id BIGINT NULL REFERENCES characters(character_id),
    target_room_id BIGINT NULL REFERENCES rooms(room_id),
    action_kind VARCHAR(32) NOT NULL,
    reason_text VARCHAR(255) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_moderation_actions_kind_not_blank CHECK (btrim(action_kind) <> ''),
    CONSTRAINT chk_moderation_actions_reason_not_blank CHECK (btrim(reason_text) <> ''),
    CONSTRAINT chk_moderation_actions_expiry CHECK (
        expires_at_utc IS NULL OR expires_at_utc > created_at_utc
    )
);

CREATE TABLE advertisement_campaigns (
    campaign_id BIGINT PRIMARY KEY,
    campaign_key VARCHAR(64) NOT NULL UNIQUE,
    campaign_name VARCHAR(128) NOT NULL,
    starts_at_utc TIMESTAMPTZ NOT NULL,
    ends_at_utc TIMESTAMPTZ NOT NULL,
    placement_kind VARCHAR(32) NOT NULL,
    target_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    creative_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_advertisement_campaigns_key_not_blank CHECK (btrim(campaign_key) <> ''),
    CONSTRAINT chk_advertisement_campaigns_name_not_blank CHECK (btrim(campaign_name) <> ''),
    CONSTRAINT chk_advertisement_campaigns_placement_kind_not_blank CHECK (btrim(placement_kind) <> ''),
    CONSTRAINT chk_advertisement_campaigns_window CHECK (ends_at_utc > starts_at_utc),
    CONSTRAINT chk_advertisement_campaigns_target_object CHECK (jsonb_typeof(target_json) = 'object'),
    CONSTRAINT chk_advertisement_campaigns_creative_object CHECK (jsonb_typeof(creative_json) = 'object')
);

CREATE INDEX idx_character_roles_character_id ON character_roles(character_id);
CREATE INDEX idx_support_calls_state ON support_calls(call_state, opened_at_utc);
CREATE INDEX idx_moderation_actions_target_character_id ON moderation_actions(target_character_id);
