# Arcturus Community Reference

This source is treated as an emulator/backend architecture reference for Epsilon. It must not become a direct runtime dependency, and source code must not be copied into Epsilon without an explicit license and architecture decision.

## Source

| Field | Value |
| --- | --- |
| Requested URL | `https://git.krews.org/morningstar/Arcturus-Community.git` |
| Working clone URL | `https://git.krews.org/morningstar/Arcturus-Community.git` |
| SSH URL tested | `git@git.krews.org:morningstar/Arcturus-Community.git` |
| Commit | `e33ac57a518d83afad5ad103599e8f3d03c37508` |
| Local reference path | `/Users/yasminluengo/Documents/Playground/reference-sources/Arcturus-Community` |
| Primary language | Java |
| Build system | Maven |
| Declared version | `3.5.5` |
| License | GPL-3.0 |
| Reference policy | `architecture_reference_no_direct_runtime_dependency` |

HTTPS works in this environment. SSH timed out on port 22, so the local reference clone was made through HTTPS.

## Repository Shape

| Metric | Value |
| --- | ---: |
| Repository size | 21 MB |
| Total files | 1,913 |
| Java files | 1,817 |
| SQL update files | 53 |
| Item interaction classes | 228 |
| Plugin event classes | 144 |
| Incoming message classes | 351 |
| Outgoing message classes | 465 |
| RCON message classes | 32 |

## Technology Observed

| Area | Evidence | Epsilon Interpretation |
| --- | --- | --- |
| Network server | Netty `Server`, `GameServer`, `RCONServer` | Epsilon's realtime gateway should keep transport concerns separate from domain services. |
| Database | MySQL Connector, HikariCP, SQL update chain | Epsilon should preserve explicit migrations and connection pooling, but use its own PostgreSQL-first schema. |
| Runtime core | `Emulator`, `GameEnvironment` | Mature hotel runtimes need deterministic boot order and shutdown order. |
| Domain managers | `HabboManager`, `RoomManager`, `CatalogManager`, `NavigatorManager`, `ItemManager`, `ModToolManager` | Useful as a domain boundary checklist for Epsilon services. |
| Packet/message layer | `PacketManager`, incoming/outgoing message families | Useful as a feature surface inventory. Do not copy packet ids, packet maps, or binary protocol behavior. |
| Room runtime | `Room`, `RoomManager`, room tasks, pathfinding, item interactions | Confirms room simulation is the core authoritative runtime, not CMS or launcher. |
| RCON | `RCONServer`, `RCONMessage` families | Admin/operator commands should be explicit, audited, and isolated from public runtime APIs. |
| Plugins | `PluginManager`, plugin events | Epsilon should keep extension points explicit and avoid modifying core runtime for every custom feature. |
| Moderation | `modtool`, sanctions, word filter, staff commands | Moderation is foundational, not a late beta feature. |
| Economy | catalog, marketplace, vouchers, subscriptions, schedulers | Wallet/catalog/trade logic needs transaction safety and auditability. |

## Domain Inventory

| Domain | Arcturus Evidence | Epsilon Target |
| --- | --- | --- |
| Identity/session | SSO ticket login, online user map, clone check | `Epsilon.Auth`, session store, launch-ticket validation. |
| Realtime transport | Netty game server | `Epsilon.Gateway` realtime plane. |
| Rooms | room manager, active rooms, layouts, room cycle | `Epsilon.Rooms` authoritative simulation. |
| Room actors | users, bots, pets, room unit state | actor runtime snapshots and room actor state. |
| Furniture | item manager, interactions, room item events | item definition catalog, placement, room item state. |
| Catalog | catalog pages/items/layouts, vouchers, target offers | catalog/shop service with ledger-backed purchases. |
| Inventory | user inventory components, item add/remove events | inventory service with ownership integrity. |
| Trading | trading incoming/outgoing messages and plugin events | trade service with escrow-like transactional flow. |
| Navigator | categories, public rooms, search | navigator/read-model service. |
| Groups/social | guilds, friends, messenger | social service split from room runtime. |
| Moderation | modtool, bans, sanctions, word filter | moderation service, admin console, audit logs. |
| Operators | RCON and console commands | privileged command model behind admin API. |
| Plugins | event bus and plugin loader | controlled extension surface, not unchecked runtime mutation. |

## Important Lessons For Epsilon

1. The emulator is runtime authority.
   The CMS and launcher should never claim room entry or gameplay state. They only authenticate, launch, and observe telemetry.

2. Rooms are active state machines.
   Room state includes actors, items, trades, games, chat policy, permissions, scheduled tasks, and persistence boundaries.

3. A large hotel runtime cannot be only HTTP.
   Control-plane APIs are useful, but gameplay needs a realtime transport and authoritative command execution.

4. Operator commands need a separate security model.
   RCON-style commands are powerful. Epsilon should route equivalent actions through audited admin APIs or internal command queues.

5. Feature flags/configuration are essential.
   Arcturus uses many runtime config toggles. Epsilon should keep this idea but make flags typed, versioned, auditable, and environment-aware.

6. Plugin/event architecture is useful but risky.
   Events should be typed and bounded. Plugins should not bypass economy, ownership, moderation, or room authority.

7. SQL history is a domain discovery source.
   The 53 SQL update files show domain evolution, but Epsilon must not import the schema wholesale.

## What To Preserve Conceptually

- Explicit boot order for runtime services.
- Separate managers/services for users, rooms, catalog, items, navigator, moderation, permissions, bots, pets, achievements, subscriptions, and calendars.
- Room cycle / simulation tick model.
- Distinct public game transport and internal operator command transport.
- Plugin/event hooks for extension.
- Strong separation between persistent user profile and room actor runtime state.
- Configurable feature flags for economy, chat, trading, moderation, pathfinding, room limits, and item behavior.

## What Not To Copy

- Proprietary or revision-specific packet ids and protocol maps.
- Brand-specific text, assets, names, or client behavior.
- Database schema as-is.
- Cryptography or login implementation as-is.
- Global static service architecture.
- Direct SQL inside domain objects.
- Un-audited operator command execution.
- Any GPL-covered source code into Epsilon unless Epsilon intentionally accepts the license consequences.

## Epsilon Implementation Guidance

| Arcturus Pattern | Epsilon Decision |
| --- | --- |
| Java monolith | Keep Epsilon modular. A monolith is acceptable early, but boundaries must remain explicit. |
| Netty TCP server | Epsilon should keep gateway transport isolated; WebSocket is still better for current launcher/client path. |
| Packet manager | Use versioned protocol adapters that translate frames into internal commands. |
| GameEnvironment managers | Use as a checklist for bounded contexts, not as a class structure to copy. |
| RCON | Implement admin command APIs with RBAC, audit logs, rate limits, and dual-control for destructive actions. |
| Plugin events | Add typed extension points only after core invariants are protected. |
| SQL updates | Use as domain inspiration; create first-class Epsilon migrations. |

## Next Engineering Actions

1. Compare Arcturus domains against Epsilon modules and identify missing bounded contexts.
2. Add acceptance tests for room entry, movement, chat, item placement, inventory grants, catalog purchase, and trade integrity.
3. Design an Epsilon operator command model based on admin APIs, not raw RCON.
4. Add a feature-flag registry for runtime limits and gameplay toggles.
5. Keep protocol compatibility work isolated in adapters. Do not pollute `Epsilon.CoreGame` with revision-specific message details.
