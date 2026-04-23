INSERT INTO accounts (
    account_id,
    login_name,
    password_hash,
    created_at_utc,
    last_login_at_utc
)
VALUES (
    1,
    'epsilon',
    'development-only-placeholder',
    NOW(),
    NOW()
);

INSERT INTO room_layouts (
    layout_code,
    door_x,
    door_y,
    door_z,
    door_rotation,
    heightmap,
    public_room_object_sets,
    club_only
)
VALUES (
    'newbie_lobby',
    0,
    0,
    0,
    2,
    '00000
00000
00000
00000
00000',
    '["epsilon_lobby"]'::jsonb,
    FALSE
);

INSERT INTO characters (
    character_id,
    account_id,
    public_id,
    username,
    motto,
    figure,
    gender,
    home_room_id,
    credits_balance,
    activity_points_balance,
    respect_points,
    daily_respect_points,
    daily_pet_respect_points
)
VALUES (
    1,
    1,
    'usr_epsilon',
    'epsilon',
    'Modern compatibility runtime',
    'hr-100-42.hd-180-1.ch-210-66.lg-270-82.sh-290-80',
    'M',
    1,
    5000,
    250,
    25,
    3,
    3
);

INSERT INTO character_subscriptions (
    character_id,
    subscription_type,
    activated_at_utc,
    expires_at_utc
)
VALUES
    (1, 'Club', NOW() - INTERVAL '7 days', NOW() + INTERVAL '23 days'),
    (1, 'Vip', NOW() - INTERVAL '7 days', NOW() + INTERVAL '23 days');

INSERT INTO rooms (
    room_id,
    room_kind,
    owner_character_id,
    name,
    description,
    category_id,
    layout_code,
    access_mode,
    access_password,
    maximum_users,
    allow_pets,
    allow_pet_eating,
    allow_walk_through,
    hide_walls,
    tags
)
VALUES (
    1,
    'Private',
    1,
    'Welcome Lounge',
    'Initial room for the first vertical slice.',
    0,
    'newbie_lobby',
    'Open',
    NULL,
    25,
    TRUE,
    FALSE,
    FALSE,
    FALSE,
    '["welcome","starter"]'::jsonb
);

INSERT INTO item_definitions (
    item_definition_id,
    public_name,
    internal_name,
    item_type_code,
    sprite_id,
    stack_height,
    can_stack,
    can_sit,
    is_walkable,
    allow_recycle,
    allow_trade,
    allow_marketplace_sell,
    allow_gift,
    allow_inventory_stack,
    interaction_type_code,
    interaction_modes_count
)
VALUES
    (
        1000,
        'Modern Sofa',
        'epsilon_sofa',
        'S',
        3001,
        1.0,
        TRUE,
        TRUE,
        FALSE,
        TRUE,
        TRUE,
        TRUE,
        TRUE,
        TRUE,
        'default',
        0
    ),
    (
        1001,
        'Portal Gate',
        'epsilon_teleporter',
        'S',
        3002,
        1.0,
        FALSE,
        FALSE,
        TRUE,
        TRUE,
        TRUE,
        FALSE,
        TRUE,
        FALSE,
        'teleporter',
        2
    );

INSERT INTO room_items (
    item_id,
    item_definition_id,
    room_id,
    floor_x,
    floor_y,
    floor_z,
    rotation,
    wall_position,
    state_data
)
VALUES
    (2000, 1000, 1, 2, 2, 0, 2, NULL, ''),
    (2001, 1001, 1, 4, 1, 0, 0, NULL, '0');

INSERT INTO pets (
    pet_id,
    owner_character_id,
    room_id,
    name,
    pet_type_id,
    race_code,
    color_code,
    experience,
    energy,
    nutrition,
    respect,
    level
)
VALUES (
    1,
    1,
    1,
    'Orbit',
    0,
    'standard',
    'FFFFFF',
    150,
    100,
    100,
    5,
    2
);
