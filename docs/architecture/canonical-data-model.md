# Canonical Data Model

The internal schema should describe Epsilon's domain clearly, even when legacy clients and old emulator databases use inconsistent naming.

## Core Aggregates

### Account

- `account_id`
- `login_name`
- `password_hash`
- `status`
- `created_at`
- `last_login_at`

### Character

- `character_id`
- `account_id`
- `name`
- `motto`
- `figure`
- `gender`
- `credits_balance`
- `activity_points_balance`
- `home_room_id`

### Session

- `session_id`
- `account_id`
- `character_id`
- `ticket`
- `remote_address`
- `created_at`
- `expires_at`

### Room

- `room_id`
- `owner_character_id`
- `name`
- `description`
- `model_key`
- `access_type`
- `max_users`
- `settings_json`

### Room Item

- `item_id`
- `definition_id`
- `owner_character_id`
- `room_id`
- `position`
- `rotation`
- `state_payload`

### Inventory Item

- `item_id`
- `owner_character_id`
- `definition_id`
- `stack_count`
- `state_payload`

## Modeling Rules

- use explicit ids and ownership fields
- keep transport packet concerns out of the schema
- avoid preserving legacy field names unless they represent true game concepts
- use JSON payloads only for truly variable item state, not as a replacement for modeling

## Import Strategy

Legacy emulator schemas should be imported into staging tables or transformed by importers. They should not dictate Epsilon's canonical model.

