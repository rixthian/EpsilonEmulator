CREATE TABLE friend_requests (
    friend_request_id BIGINT PRIMARY KEY,
    requester_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    target_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    request_state VARCHAR(16) NOT NULL DEFAULT 'pending',
    created_at_utc TIMESTAMPTZ NOT NULL,
    responded_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_friend_requests_not_self CHECK (requester_character_id <> target_character_id),
    CONSTRAINT chk_friend_requests_state CHECK (request_state IN ('pending', 'accepted', 'declined', 'expired'))
);

CREATE TABLE friendships (
    friendship_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    friend_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    created_at_utc TIMESTAMPTZ NOT NULL,
    favourite_position INT NULL,
    CONSTRAINT chk_friendships_not_self CHECK (character_id <> friend_character_id)
);

CREATE TABLE messenger_messages (
    message_id BIGINT PRIMARY KEY,
    sender_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    receiver_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    body_text TEXT NOT NULL,
    sent_at_utc TIMESTAMPTZ NOT NULL,
    delivered_at_utc TIMESTAMPTZ NULL,
    read_at_utc TIMESTAMPTZ NULL,
    CONSTRAINT chk_messenger_messages_body_not_blank CHECK (btrim(body_text) <> '')
);

CREATE TABLE groups (
    group_id BIGINT PRIMARY KEY,
    owner_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    group_name VARCHAR(64) NOT NULL UNIQUE,
    description TEXT NOT NULL DEFAULT '',
    badge_code VARCHAR(64) NULL,
    room_id BIGINT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_groups_name_not_blank CHECK (btrim(group_name) <> '')
);

CREATE TABLE group_members (
    group_id BIGINT NOT NULL REFERENCES groups(group_id),
    character_id BIGINT NOT NULL REFERENCES characters(character_id),
    member_role VARCHAR(16) NOT NULL DEFAULT 'member',
    joined_at_utc TIMESTAMPTZ NOT NULL,
    is_favourite BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (group_id, character_id),
    CONSTRAINT chk_group_members_role CHECK (member_role IN ('owner', 'admin', 'member', 'pending'))
);

CREATE TABLE room_events (
    room_event_id BIGINT PRIMARY KEY,
    room_id BIGINT NOT NULL,
    host_character_id BIGINT NOT NULL REFERENCES characters(character_id),
    event_name VARCHAR(128) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    event_category VARCHAR(32) NOT NULL,
    starts_at_utc TIMESTAMPTZ NOT NULL,
    ends_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_room_events_name_not_blank CHECK (btrim(event_name) <> ''),
    CONSTRAINT chk_room_events_category_not_blank CHECK (btrim(event_category) <> ''),
    CONSTRAINT chk_room_events_end_after_start CHECK (ends_at_utc > starts_at_utc)
);

CREATE INDEX idx_friend_requests_target_character_id ON friend_requests(target_character_id);
CREATE UNIQUE INDEX idx_friendships_pair ON friendships(character_id, friend_character_id);
CREATE INDEX idx_messenger_messages_receiver_character_id ON messenger_messages(receiver_character_id, sent_at_utc);
CREATE INDEX idx_group_members_character_id ON group_members(character_id);
