CREATE TABLE IF NOT EXISTS character_chat_filter_preferences (
    character_id BIGINT PRIMARY KEY REFERENCES characters(character_id),
    mute_bots BOOLEAN NOT NULL DEFAULT FALSE,
    mute_pets BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    updated_by VARCHAR(64) NOT NULL,
    CONSTRAINT chk_character_chat_filter_preferences_updated_by_not_blank CHECK (btrim(updated_by) <> '')
);
