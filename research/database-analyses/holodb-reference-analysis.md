# HoloDB Reference Analysis

Source analyzed:

- [holodb.sql](/Users/yasminluengo/Downloads/holodb.sql)

## Summary

This database is useful as a domain-discovery artifact, not as a schema to inherit.

It helps identify which feature areas existed in one legacy stack:

- users and sessions
- rooms and room models
- furniture instances and item templates
- catalogue and deals
- messenger
- groups
- moderation and staff logging
- system configuration and localized strings
- CMS and homes features

## What Is Useful For Epsilon

### 1. Domain surface mapping

The schema confirms a broad product surface across these table families:

- `users*`
- `rooms*`
- `room_modeldata*`
- `furniture*`
- `catalogue*`
- `messenger*`
- `groups*`
- `system*`

This is useful for prioritizing bounded contexts in Epsilon.

### 2. Room model concepts

The `room_modeldata` table is useful as evidence of old room-model concerns:

- door position
- heightmap
- public room item sets
- swimming pool flag
- special cast/emitter metadata

These are valid concepts to preserve, but the storage shape should be redesigned.

### 3. Furniture instance concepts

The `furniture` table is useful for identifying runtime concerns:

- item definition id
- owner id
- room id vs inventory state
- floor coordinates
- wall position
- state payload
- teleporter linkage
- sound machine metadata

These concepts should be split into cleaner aggregates in Epsilon.

### 4. System configuration taxonomy

The `system_config` table shows the type of operational settings legacy emulators exposed:

- network ports and backlog
- language
- wordfilter toggle
- trading toggle
- recycler settings
- room timings
- navigator limits

This is useful for building a modern typed configuration surface, but not for using a stringly typed key/value table as the core runtime model.

### 5. String catalog concept

The `system_strings` table is useful as evidence that dynamic localized server strings matter.

Epsilon should keep this idea, but move to:

- versioned language resources
- structured localization keys
- explicit fallback behavior

## What Should Not Be Reused

### 1. The schema itself

This schema is not a safe foundation for a modern emulator because it is:

- entirely `MyISAM`
- missing foreign keys
- weakly typed in many critical areas
- overloaded with unrelated CMS and hotel concerns
- dependent on string timestamps and string flags

### 2. CMS coupling

There are 17 `cms_*` tables. They should not shape Epsilon's core architecture.

Examples:

- forum
- minimail
- homes
- CMS content/settings

Those may become separate products or adapters later, but they should not pollute the runtime core.

### 3. Legacy data hygiene

The seed data contains low-quality and unsuitable content, including offensive room and bot text. That makes the dump unsafe to import as content.

Only structural concepts should be learned from this file. Raw content should not be adopted.

### 4. User/session model

The `users` table mixes too many concerns:

- identity
- profile
- progression
- session ticket
- online status
- moderation-adjacent state

Epsilon should split these into clearer models such as:

- account
- character
- session
- wallet
- profile
- subscription

## Structural Problems

Observed issues:

- 59 tables in one monolithic schema
- nearly every table uses `ENGINE=MyISAM`
- no foreign key relationships
- many booleans represented as `enum('0','1')`
- timestamps stored as `varchar`
- ownership sometimes stored by username instead of id
- system configuration stored as weak key/value text pairs
- messenger and moderation logs stored with weak consistency guarantees

## Recommended Epsilon Actions

### Keep as concepts

- room models
- furniture instance state
- catalogue structure
- user favourites
- badges
- subscriptions
- group membership
- system/localized string catalog

### Redesign completely

- auth and sessions
- CMS integration
- moderation persistence
- messenger storage
- room rights and bans
- recycler state
- system configuration storage

### Add to Epsilon roadmap

This reference supports creating these future Epsilon artifacts:

1. room model specification
2. item instance state model
3. configuration key taxonomy
4. separation plan for account vs character vs session
5. importer rules for legacy room/furniture data

## Bottom Line

`holodb.sql` is worth keeping as a reference for feature discovery and concept extraction.

It is not worth inheriting as:

- runtime schema
- migration target
- content seed
- operational design

