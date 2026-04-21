# uberEmu2 Analysis

Source analyzed:

- [uberEmu2](/Users/yasminluengo/Downloads/uberemu-source_including_ubercms/uberEmu2)
- [phoenix 3.11.0.sql](/Users/yasminluengo/Downloads/phoenix%203.11.0.sql)

## What This Artifact Is

`uberEmu2` is a source-available C# emulator package with an attached CMS and SQL schema.

Unlike binary-only packages, this one exposes:

- packet handler registration
- request parsing conventions
- room-loading sequence
- catalog purchase validation
- navigator serialization
- inventory and trade flows
- launcher contract through the CMS

That makes it one of the best reference artifacts for understanding how a legacy hotel server actually moved from packet input to domain behavior.

## Why It Matters

This project is valuable for a different reason than Phoenix.

- Phoenix is stronger as evidence of product breadth and later schema growth.
- uberEmu2 is stronger as evidence of request flow, room bootstrap, and packet-domain decomposition.

For Epsilon, that means uberEmu2 is more useful for:

- protocol surface design
- application service boundaries
- room loading state transitions
- purchase pipeline structure
- rights and subscription capability handling

## High-Value Signals

### 1. Request handling is grouped by business surface

`GameClientMessageHandler` is a single dispatch point backed by numeric packet registration, but the registrations are split into request-focused files:

- `Handshake.cs`
- `Catalog.cs`
- `Navigator.cs`
- `Rooms.cs`
- `Users.cs`
- `Messenger.cs`
- `Help.cs`
- `Global.cs`

That is a strong hint for Epsilon.

The useful lesson is not "one giant handler class". The useful lesson is that the packet surface naturally clusters into hotel capabilities:

- connection and session bootstrap
- catalog and economy
- navigator discovery
- room lifecycle and interaction
- identity and avatar state
- social and support tooling

### 2. Packet families are very explicit

The request registry exposes a broad message surface with clear operational groupings.

Examples:

- Handshake:
  - `206`
  - `415`
- Catalog:
  - `101`
  - `102`
  - `100`
  - `472`
  - marketplace `30xx`
- Navigator:
  - `151`
  - `380`
  - `385`
  - `430-439`
- Rooms:
  - `391`
  - `2`
  - `59`
  - `75`
  - `90`
  - `73`
  - `71`
  - `72`
  - `69`
  - `68`
  - `402`
  - pet handlers `3001-3005`

This matters because it confirms that a hotel-compatible runtime is not just "login and room enter". The packet surface already implies a broad service boundary map.

### 3. Room loading is a multi-step workflow, not one action

The room flow in `Messages/Requests/Rooms.cs` is especially important.

The sequence is roughly:

1. room open request for private or public room
2. `PrepareRoomForUser`
3. access validation
4. room lazy-load if needed
5. loading state set on the session
6. room path message emitted
7. `LoadRoomForUser`
8. room model and room data packets emitted
9. heightmap and relative heightmap emitted
10. static furni map emitted
11. room items emitted
12. users emitted
13. rights/rating/event state emitted
14. room entry completes

This is one of the strongest signals in the whole analysis.

For Epsilon, room entry should become a formal state machine rather than a loose pile of handler methods.

### 4. Public rooms have a distinct content identity

uberEmu2 keeps public-room-specific data in several places:

- `rooms.public_ccts`
- `navigator_publics`
- `room_models.public_items`
- `RoomData.CCTs`
- `GetPub()` returning CCT/package information

This confirms a design rule already emerging from Phoenix:

- room layout identity is not enough
- public-room visual/content identity must be modeled explicitly

### 5. Catalog purchasing already behaves like a policy pipeline

`Catalog.HandlePurchase(...)` is legacy code, but the shape is valuable.

The flow validates:

- page visibility and enablement
- rank requirement
- subscription requirement
- item existence
- gift eligibility
- receiver existence
- credit and point balances
- interaction-specific extra data

Then it normalizes extra data by interaction type:

- `pet`
- `roomeffect`
- `postit`
- `dimmer`
- `trophy`
- default item behavior

This is one of the clearest pieces of reusable logic shape in the project.

For Epsilon, purchases should be implemented as:

- offer resolution
- access policy evaluation
- price and wallet check
- per-interaction payload validation
- delivery plan execution
- inventory and wallet events

### 6. Rights are capability-based, not only rank-based

`RoleManager` loads:

- `ranks`
- `fuserights`
- `fuserights_subs`

This is better than Phoenix's very wide permission matrices.

The legacy implementation is simple, but the concept is correct:

- base capabilities from rank
- additive capabilities from subscriptions

Epsilon should preserve that idea while translating it to:

- typed capabilities
- policy evaluation
- optional per-user grants or denies

### 7. Inventory and room placement are still too split, but operationally clear

uberEmu2 uses:

- `user_items`
- `room_items`
- `user_pets`
- `user_presents`

That is not the data model Epsilon should inherit.

However, operationally it is useful because it makes the transitions explicit:

- purchase delivers to inventory
- placement moves an item into room state
- trade operates on inventory ownership
- pet placement moves a pet from inventory presence to room presence

Phoenix remains the stronger signal for a future canonical item-instance model, but uberEmu2 is the stronger signal for the lifecycle transitions.

### 8. Launcher contract is clearly visible

`ubercms/client.php` exposes the web-to-client bootstrap surface:

- `sso_ticket`
- `flash_base`
- `flash_client_url`
- `forwardType`
- `forwardId`

This is useful as a product contract, not as implementation guidance.

Epsilon should keep the launcher separate from the runtime, but it should explicitly model this bootstrap contract instead of letting it leak through ad hoc templates.

### 9. Wire protocol parsing is explicit enough to study

`ClientMessage.cs` and `OldWireEncoding.cs` show the legacy protocol mechanics:

- message id header parsing
- fixed-length string values
- base64 integer decoding
- VL64 integer encoding and decoding
- pointer-based body consumption

Epsilon should not inherit these classes, but they are useful for confirming:

- which primitive field encodings exist
- how old clients expect values to be framed
- where packet parsing should stay isolated from domain code

## What Phoenix Adds To The Picture

Phoenix 3.11.0 complements uberEmu2 instead of replacing it.

Together they draw a much clearer picture.

uberEmu2 contributes more to:

- packet families
- request sequencing
- room bootstrap logic
- trade workflow
- launcher contract
- capability-style rights

Phoenix contributes more to:

- late-era product breadth
- unified item persistence direction
- richer interaction taxonomy
- public-room asset identity
- capability-heavy authorization surface
- wider user progression model

## Best Combined Lessons For Epsilon

### Keep As Modern Design Ideas

- packet handlers grouped by business capability
- room entry as a formal workflow
- purchase validation as a pipeline
- public-room asset identity as first-class content
- capability-based authorization
- explicit separation between launcher, gateway, protocol, game, and content

### Reject

- monolithic handler classes
- direct SQL inside request handlers
- string-concatenated queries
- raw rank checks scattered everywhere
- PHP launcher logic coupled to templates
- split inventory persistence as the long-term canonical model
- thread-per-concern architecture and custom lifecycle glue

## Concrete Design Impact On Epsilon

This analysis increases the priority of:

1. a formal request registry in the protocol layer
2. a room entry state machine in the rooms application layer
3. a purchase validation pipeline in the economy/catalog layer
4. a typed capability model in auth/authorization
5. a launcher bootstrap contract between web and runtime
6. canonical content identities for public-room asset packages

## Bottom Line

`uberEmu2` is one of the strongest functional references in the corpus because it shows how a legacy emulator decomposed hotel behavior across packets, room loading, inventory, purchases, and rights.

Its direct implementation is not suitable for reuse.

Its structural lessons are useful.

For Epsilon, the right translation is:

- keep the flow shapes
- discard the runtime architecture
- keep the business boundaries
- discard the SQL and transport coupling

