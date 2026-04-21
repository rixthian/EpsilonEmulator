# duckdb Reference Analysis

Source analyzed:

- [duckdb.sql](/Users/yasminluengo/Downloads/duckdb.sql)

## What This Artifact Is

This is a MySQL dump from a smaller legacy emulator/CMS stack dated around mid-2011.

The schema contains `26` tables and is notably narrower than:

- Phoenix 3.11.0
- uberEmu2
- Holograph R59

It appears to represent a hotel implementation with a smaller operational surface and a simpler schema split.

## High-Level Character

This database sits in an interesting middle point.

It is more primitive than later mature schemas in several areas:

- no subscriptions model
- no pets model
- no moderation subsystem
- no explicit navigator/public-room content richness
- no capability/rights matrix
- no separate external text/variable system

At the same time, it contains two signals that still matter for Epsilon:

- a unified `items` table
- a fairly rich furniture `interaction_type` taxonomy

## Tables Present

Main table families found:

- `users`
- `items`
- `furniture`
- `catalog_pages`
- `catalog_items`
- `room_models`
- `private_rooms`
- `public_rooms`
- `private_categorys`
- `public_categorys`
- `achievements`
- `achievements_data`
- `user_achievements`
- `user_badges`
- `user_effects`
- `user_favorites`
- `user_friends`
- `user_friendrequests`
- `user_tags`
- `site_news`
- `site_recommend`

## What Is Valuable For Epsilon

### 1. Canonical item-instance direction

The `items` table stores both inventory and placed-room state:

- `user_id`
- `room_id`
- `base_id`
- `x`
- `y`
- `rot`
- `z`
- `wall_pos`
- `extra_data`

That reinforces a design direction already visible in Phoenix:

- the long-term persistence model should favor a canonical item-instance aggregate
- inventory vs room placement should be lifecycle states, not fundamentally different entity types

This is the strongest technical signal in the dump.

### 2. Furniture interaction taxonomy

The `furniture` table contains `57` interaction values, including:

- core interactions such as `default`, `gate`, `postit`, `roomeffect`, `dimmer`, `trophy`, `teleport`, `pet`, `roller`
- game interactions such as `ball`, `bb_*`, `red_goal`, `blue_goal`, `counter`
- early wired-related entries such as:
  - `wired`
  - `wf_trg_onsay`
  - `wf_act_saymsg`
  - `wf_trg_enterroom`
  - `wf_act_moveuser`
  - `wf_act_togglefurni`
  - `wf_trg_furnistate`
  - `wf_trg_onfurni`
  - `wf_trg_offfurni`
  - `wf_trg_gameend`
  - `wf_trg_gamestart`
  - `wf_trg_timer`
  - `wf_act_givepoints`
  - `wf_trg_attime`
  - `wf_trg_atscore`
  - `wf_act_moverotate`

This is useful because it confirms the content-driven nature of interaction metadata even in a smaller schema.

For Epsilon, this remains a content concern, not a runtime enum hardcoded everywhere.

### 3. Room layout remains a separate concept

`room_models` is simple, but still preserves:

- model key
- map
- door coordinates
- door rotation

This is consistent with every serious reference seen so far.

Epsilon should continue treating room layout identity as a first-class content model.

### 4. Private/public room separation exists, but in a weak form

Instead of a single richer room table, this schema uses:

- `private_rooms`
- `public_rooms`
- `private_categorys`
- `public_categorys`

This is not a shape Epsilon should reuse.

However, it does confirm a product truth:

- public room visibility and listing is not identical to private room ownership and access

The concept is valid.
The schema shape is not.

### 5. Progression and social layers are already split out

There are separate tables for:

- achievements
- badges
- avatar effects
- favorites
- friends
- tags

This is limited compared to later projects, but it still reinforces that user state should not be collapsed into one oversized `users` record.

## What Is Weak Or Not Worth Reusing

### 1. Very incomplete product surface

Compared with stronger references, the schema is missing major subsystems:

- subscriptions
- pets
- moderation
- marketplace
- messenger maturity
- room rights maturity
- public-room asset identity
- support tooling
- text and variable configuration systems

This means it is not a strong reference for full product scope.

### 2. Primitive room modeling

The room model is split between `private_rooms` and `public_rooms` in a way that loses flexibility.

The `public_rooms` table is especially weak:

- it mostly links a private room id to a public category and image
- it does not model richer public-room content identity

Compared with Phoenix or uberEmu2, this is a step backward.

### 3. Low-quality schema hygiene

The schema is fully legacy in the usual ways:

- `MyISAM`
- no foreign keys
- many `enum('0','1')`
- timestamps and dates stored inconsistently
- owner stored as username
- visible CMS content mixed into the same schema

### 4. Broken or low-value quest modeling

`quests`, `quests_categorys`, and `quest_categorys` are too weak and underspecified to be a serious reference for Epsilon's quest domain.

They should be ignored as design input.

## Comparison Against Other References

### Stronger than duckdb

- Phoenix 3.11.0
  Better for product breadth, public rooms, permissions, and interaction coverage.
- uberEmu2
  Better for request flow, room loading, launcher contract, and capability model.
- Holograph R59
  Better for mature R59 feature surface.

### Still useful despite being weaker

duckdb remains useful because it reinforces two independent signals already visible elsewhere:

1. canonical item-instance persistence
2. content-driven interaction metadata

That makes it a corroborating reference, not a leading one.

## Concrete Design Impact On Epsilon

This dump supports the following Epsilon decisions:

1. keep item persistence centered on a canonical item-instance model
2. keep interaction metadata versionable and content-driven
3. keep room layouts as distinct content records
4. keep public-room listing as a distinct domain from private-room ownership

It does not justify changing the current Epsilon direction on:

- authorization
- public-room asset package identity
- subscriptions
- moderation
- launcher design

## Bottom Line

`duckdb.sql` is not one of the strongest references in the corpus.

Its main value is confirmatory:

- item instances should be unified
- furniture interactions should stay content-driven
- room layouts remain first-class

Everything else in the dump is either too narrow, too legacy-bound, or too incomplete to drive Epsilon's design.

