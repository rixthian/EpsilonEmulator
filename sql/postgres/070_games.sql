CREATE TABLE game_definitions (
    game_id BIGINT PRIMARY KEY,
    game_key VARCHAR(64) NOT NULL UNIQUE,
    display_name VARCHAR(128) NOT NULL,
    queue_kind VARCHAR(32) NOT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_game_definitions_key_not_blank CHECK (btrim(game_key) <> ''),
    CONSTRAINT chk_game_definitions_display_name_not_blank CHECK (btrim(display_name) <> ''),
    CONSTRAINT chk_game_definitions_queue_kind_not_blank CHECK (btrim(queue_kind) <> '')
);

CREATE TABLE game_venues (
    game_venue_id BIGINT PRIMARY KEY,
    game_id BIGINT NOT NULL REFERENCES game_definitions(game_id),
    room_id BIGINT NOT NULL REFERENCES rooms(room_id),
    venue_key VARCHAR(64) NOT NULL UNIQUE,
    map_key VARCHAR(64) NULL,
    team_mode VARCHAR(16) NOT NULL DEFAULT 'solo',
    max_players INT NOT NULL,
    CONSTRAINT chk_game_venues_key_not_blank CHECK (btrim(venue_key) <> ''),
    CONSTRAINT chk_game_venues_team_mode CHECK (team_mode IN ('solo', 'teams')),
    CONSTRAINT chk_game_venues_max_players CHECK (max_players > 0)
);

CREATE TABLE game_sessions (
    game_session_id BIGINT PRIMARY KEY,
    game_venue_id BIGINT NOT NULL REFERENCES game_venues(game_venue_id),
    session_state VARCHAR(16) NOT NULL DEFAULT 'waiting',
    started_at_utc TIMESTAMPTZ NOT NULL,
    ended_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_game_sessions_state CHECK (session_state IN ('waiting', 'running', 'finished', 'cancelled')),
    CONSTRAINT chk_game_sessions_end_after_start CHECK (
        ended_at_utc IS NULL OR ended_at_utc >= started_at_utc
    )
);

CREATE TABLE game_teams (
    game_team_id BIGINT PRIMARY KEY,
    game_session_id BIGINT NOT NULL REFERENCES game_sessions(game_session_id),
    team_key VARCHAR(32) NOT NULL,
    display_name VARCHAR(64) NOT NULL,
    score_value INT NOT NULL DEFAULT 0,
    CONSTRAINT chk_game_teams_team_key_not_blank CHECK (btrim(team_key) <> ''),
    CONSTRAINT chk_game_teams_display_name_not_blank CHECK (btrim(display_name) <> ''),
    CONSTRAINT chk_game_teams_score_value CHECK (score_value >= 0)
);

CREATE TABLE game_players (
    game_session_id BIGINT NOT NULL REFERENCES game_sessions(game_session_id),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    game_team_id BIGINT NULL REFERENCES game_teams(game_team_id),
    joined_at_utc TIMESTAMPTZ NOT NULL,
    left_at_utc TIMESTAMPTZ NULL,
    score_value INT NOT NULL DEFAULT 0,
    PRIMARY KEY (game_session_id, character_id),
    CONSTRAINT chk_game_players_score_value CHECK (score_value >= 0)
);

CREATE TABLE snowstorm_maps (
    snowstorm_map_id BIGINT PRIMARY KEY,
    map_key VARCHAR(64) NOT NULL UNIQUE,
    venue_key VARCHAR(64) NOT NULL,
    placements_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    spawn_clusters_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    snowmachines_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT chk_snowstorm_maps_key_not_blank CHECK (btrim(map_key) <> ''),
    CONSTRAINT chk_snowstorm_maps_venue_key_not_blank CHECK (btrim(venue_key) <> ''),
    CONSTRAINT chk_snowstorm_maps_placements_array CHECK (jsonb_typeof(placements_json) = 'array'),
    CONSTRAINT chk_snowstorm_maps_spawn_clusters_array CHECK (jsonb_typeof(spawn_clusters_json) = 'array'),
    CONSTRAINT chk_snowstorm_maps_snowmachines_array CHECK (jsonb_typeof(snowmachines_json) = 'array')
);

CREATE INDEX idx_game_venues_game_id ON game_venues(game_id);
CREATE INDEX idx_game_sessions_game_venue_id ON game_sessions(game_venue_id);
CREATE INDEX idx_game_players_character_id ON game_players(character_id);
