CREATE TABLE accounts (
    account_id BIGINT PRIMARY KEY,
    login_name VARCHAR(64) NOT NULL UNIQUE,
    normalized_login_name VARCHAR(64) NOT NULL UNIQUE,
    email_address VARCHAR(320) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    account_status VARCHAR(16) NOT NULL DEFAULT 'active',
    created_at_utc TIMESTAMPTZ NOT NULL,
    last_login_at_utc TIMESTAMPTZ NULL,
    last_password_change_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_accounts_login_name_not_blank CHECK (btrim(login_name) <> ''),
    CONSTRAINT chk_accounts_normalized_login_name_not_blank CHECK (btrim(normalized_login_name) <> ''),
    CONSTRAINT chk_accounts_email_address_not_blank CHECK (btrim(email_address) <> ''),
    CONSTRAINT chk_accounts_password_hash_not_blank CHECK (btrim(password_hash) <> ''),
    CONSTRAINT chk_accounts_status CHECK (account_status IN ('active', 'banned', 'disabled', 'pending')),
    CONSTRAINT chk_accounts_last_login_after_create CHECK (
        last_login_at_utc IS NULL OR last_login_at_utc >= created_at_utc
    )
);

CREATE TABLE account_security_events (
    security_event_id BIGINT PRIMARY KEY,
    account_id BIGINT NOT NULL REFERENCES accounts(account_id),
    event_type VARCHAR(32) NOT NULL,
    remote_address VARCHAR(64) NOT NULL,
    user_agent VARCHAR(255) NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    detail_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT chk_account_security_events_type_not_blank CHECK (btrim(event_type) <> ''),
    CONSTRAINT chk_account_security_events_remote_address_not_blank CHECK (btrim(remote_address) <> ''),
    CONSTRAINT chk_account_security_events_detail_object CHECK (jsonb_typeof(detail_json) = 'object')
);

CREATE TABLE characters (
    character_id BIGINT PRIMARY KEY,
    account_id BIGINT NOT NULL REFERENCES accounts(account_id),
    public_id VARCHAR(64) NOT NULL UNIQUE,
    username VARCHAR(32) NOT NULL UNIQUE,
    normalized_username VARCHAR(32) NOT NULL UNIQUE,
    motto VARCHAR(128) NOT NULL DEFAULT '',
    gender VARCHAR(1) NOT NULL,
    figure_code VARCHAR(255) NOT NULL,
    home_room_id BIGINT NULL,
    online_status VARCHAR(16) NOT NULL DEFAULT 'offline',
    created_at_utc TIMESTAMPTZ NOT NULL,
    last_online_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_characters_public_id_not_blank CHECK (btrim(public_id) <> ''),
    CONSTRAINT chk_characters_username_not_blank CHECK (btrim(username) <> ''),
    CONSTRAINT chk_characters_normalized_username_not_blank CHECK (btrim(normalized_username) <> ''),
    CONSTRAINT chk_characters_gender CHECK (gender IN ('M', 'F', 'U')),
    CONSTRAINT chk_characters_online_status CHECK (online_status IN ('offline', 'online', 'busy', 'away'))
);

CREATE TABLE character_preferences (
    character_id BIGINT PRIMARY KEY REFERENCES characters(character_id),
    language_code VARCHAR(8) NOT NULL DEFAULT 'en',
    allow_friend_requests BOOLEAN NOT NULL DEFAULT TRUE,
    allow_profile_view BOOLEAN NOT NULL DEFAULT TRUE,
    allow_room_invites BOOLEAN NOT NULL DEFAULT TRUE,
    ui_settings_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_character_preferences_language_code_not_blank CHECK (btrim(language_code) <> ''),
    CONSTRAINT chk_character_preferences_ui_settings_object CHECK (jsonb_typeof(ui_settings_json) = 'object')
);

CREATE TABLE character_sessions (
    session_id UUID PRIMARY KEY,
    account_id BIGINT NOT NULL REFERENCES accounts(account_id),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    session_ticket VARCHAR(128) NOT NULL UNIQUE,
    remote_address VARCHAR(64) NOT NULL,
    started_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NOT NULL,
    ended_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_character_sessions_ticket_not_blank CHECK (btrim(session_ticket) <> ''),
    CONSTRAINT chk_character_sessions_remote_address_not_blank CHECK (btrim(remote_address) <> ''),
    CONSTRAINT chk_character_sessions_expiry CHECK (expires_at_utc > started_at_utc),
    CONSTRAINT chk_character_sessions_end_after_start CHECK (
        ended_at_utc IS NULL OR ended_at_utc >= started_at_utc
    )
);

CREATE INDEX idx_account_security_events_account_id ON account_security_events(account_id);
CREATE INDEX idx_characters_account_id ON characters(account_id);
CREATE UNIQUE INDEX idx_characters_public_id_lower ON characters(lower(public_id));
CREATE INDEX idx_character_sessions_account_id ON character_sessions(account_id);
CREATE INDEX idx_character_sessions_character_id ON character_sessions(character_id);
CREATE INDEX idx_character_sessions_expires_at_utc ON character_sessions(expires_at_utc);
