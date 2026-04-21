# Phoenix 3.11.0 Analysis

Source analyzed:

- [phoenix 3.11.0.sql](/Users/yasminluengo/Downloads/phoenix%203.11.0.sql)

## What This Artifact Is

This is a large MySQL dump from a later Phoenix line.

The schema contains 63 tables and is more useful than earlier binary-only packages because it shows:

- mature room/public-room modeling
- richer furniture interaction taxonomy
- moderation and permissions growth
- content-heavy catalog and text systems
- social, quest, pet, badge, and subscription domains

## Why It Matters

Compared with earlier references, this dump gives stronger signals about what a late Flash-era emulator considered operationally important.

It is especially useful for:

- public room content structure
- capability-based permissions
- unified item instance modeling
- formalized interaction types
- user progression split across multiple tables

## High-Value Signals

### 1. Furniture definitions became a dense capability catalog

The `furniture` table is a strong reference point because it centralizes:

- dimensions
- stack height
- physical flags
- trading/recycling/gifting flags
- sprite id
- interaction type
- interaction mode count

Most importantly, `interaction_type` is already acting like a content taxonomy, not just a runtime enum.

It includes:

- standard furniture interactions
- game interactions
- pet interactions
- wired triggers
- wired actions
- wired conditions

This strongly supports Epsilon keeping interaction metadata in content definitions rather than scattering it across room logic.

### 2. Phoenix uses a unified item instance table

The `items` table combines inventory and placed room state into one instance model:

- `user_id`
- `room_id`
- `base_item`
- `extra_data`
- floor position
- wall position

This confirms the earlier Phoenix 3.8.1 signal: item persistence wants a canonical item instance model, not two unrelated schemas for inventory and room placement.

### 3. Public rooms have both a room model and a visual asset package identity

The `rooms` table stores:

- `roomtype`
- `model_name`
- `public_ccts`

That is important.

For Epsilon, a public room should not be modeled as only:

- room metadata
- layout code

It also needs:

- a visual asset package identity
- a separable public-room content key

Phoenix makes that distinction explicit with `public_ccts`.

### 4. Room models preserve structural fidelity

The `room_models` table remains one of the most useful legacy concepts:

- door position
- door direction
- heightmap
- public items
- club-only flag

This is still directly relevant to Epsilon's canonical room layout model.

### 5. Navigator public rooms are a separate content domain

The `navigator_publics` table models:

- caption
- order
- banner type
- image key
- image type
- linked room id
- category grouping

That is a good signal that navigator public-room entries should be treated as content records, not derived on the fly from the room table alone.

### 6. Permissions moved toward capability matrices

`permissions_ranks`, `permissions_users`, and `permissions_vip` are ugly schemas, but the concept is useful:

- access should be capability-based
- rank is not enough on its own
- user overrides exist
- VIP can be modeled as a feature set

Epsilon should not copy the hundreds of boolean columns, but it should preserve the idea of explicit capabilities.

### 7. Progression is already partially decomposed

Phoenix still has legacy table design, but it splits progression concerns across:

- `user_stats`
- `user_achievements`
- `user_badges`
- `user_subscriptions`
- `user_quests`
- `user_tags`
- `user_wardrobe`

This reinforces Epsilon's choice not to collapse everything into one `users` record.

## What Epsilon Should Take

- a strong content-driven furniture definition model
- canonical item instances
- explicit public-room asset package identity
- navigator public entries as first-class content
- capability-based authorization
- progression split into bounded aggregates

## What Epsilon Should Reject

- `MyISAM`
- enum-string sprawl
- giant permission tables with one column per command
- owner-by-username in room records
- timestamps as loose numbers or strings
- CMS/runtime table mixing

## Concrete Design Impact On Epsilon

Phoenix 3.11.0 changes the priority of these Epsilon concerns:

1. Public-room content must include an asset package key, not just a room layout code.
2. The future persistence model should converge on a canonical item instance aggregate.
3. Authorization should use capabilities and policy evaluation, not only rank values.
4. Navigator public-room entries need their own repository/model path.
5. Furniture interaction metadata should stay content-driven and versionable.

## Bottom Line

Phoenix 3.11.0 is one of the strongest schema references so far for late Flash-era product shape.

Its direct implementation quality is legacy-bound, but its domain signals are valuable:

- public rooms are content-rich
- items are unified
- permissions are capability-shaped
- interaction taxonomies are large and central
