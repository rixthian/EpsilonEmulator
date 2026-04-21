# Client Hotel Flow Blueprint

This document defines how Epsilon should structure the client-facing hotel flow at the application layer.

It is intentionally independent from any legacy implementation.

## Purpose

The goal is to keep three concerns separate:

- packet parsing and encoding
- application workflows
- hotel domain state

Legacy emulators usually mixed these concerns into one handler layer.

Epsilon should not.

## Runtime Layers

### Gateway

Responsible for:

- socket lifecycle
- connection throttling
- encryption handoff
- framing
- session-bound packet input and output

The gateway should not know hotel business rules.

### Protocol

Responsible for:

- packet id registry
- packet schema mapping
- decoding client payloads into typed commands
- encoding typed responses into client packets

The protocol layer should not perform persistence or hotel decisions.

### Application Flows

Responsible for:

- orchestrating hotel actions
- checking policies
- loading aggregates
- emitting response models

This is where the hotel behavior lives.

### Domain

Responsible for:

- room rules
- item state rules
- inventory ownership
- subscriptions
- pets
- navigator and catalog content

The domain should not know packet ids.

## Primary Flow Families

### Session Bootstrap Flow

Input shape:

- handshake
- session parameters
- ticket login
- identity load

Application responsibilities:

- validate session ticket
- resolve account and character
- load subscriptions and permissions
- construct hotel bootstrap snapshot

Output shape:

- session accepted
- character context
- capabilities
- wallet and profile summary
- initial hotel configuration

### Navigator Flow

Input shape:

- flat categories request
- public-room listing request
- search request
- room details request
- favorites and recent rooms request

Application responsibilities:

- resolve navigator category trees
- apply visibility policy
- materialize room summary cards
- attach public-room asset identities where relevant

Output shape:

- navigator sections
- public-room entries
- room summary list
- room detail snapshot

### Room Entry Flow

Room entry must be a state machine.

It should not be a single method.

Suggested states:

1. `Requested`
2. `AccessValidated`
3. `RoomInstanceReady`
4. `LayoutPrepared`
5. `ContentPrepared`
6. `OccupantPrepared`
7. `Entered`
8. `Failed`

Suggested responsibilities by stage:

1. `Requested`
   - capture target room id
   - capture entry mode
   - capture optional password or forward data

2. `AccessValidated`
   - check room existence
   - check room kind
   - check access state
   - check bans, locks, password, queue rules, spectator mode

3. `RoomInstanceReady`
   - load room aggregate
   - start runtime room instance if not active

4. `LayoutPrepared`
   - resolve room layout
   - resolve public-room asset package if needed
   - prepare heightmap and static layout payloads

5. `ContentPrepared`
   - load floor items
   - load wall items
   - load room settings state
   - load event and rating state

6. `OccupantPrepared`
   - add current user
   - collect current visible occupants
   - collect transient actor state such as dance, carry, sleep, effect

7. `Entered`
   - emit final packet set for room readiness

8. `Failed`
   - emit access or load failure response
   - clear pending room state

### Room Interaction Flow

Input shape:

- move avatar
- talk, whisper, shout
- place item
- move item
- rotate item
- use item
- apply effect
- give respect

Application responsibilities:

- verify room presence
- resolve actor and room instance
- verify rights or capabilities
- execute interaction-specific policy
- emit room updates

This flow family should be split into separate command handlers, not one giant room handler.

### Catalog Purchase Flow

This should be a pipeline.

Suggested stages:

1. resolve page and offer
2. validate availability and visibility
3. validate capability or subscription requirements
4. validate wallets
5. validate typed payload for the offer
6. build delivery plan
7. commit wallet and delivery transaction
8. emit purchase result and inventory updates

Typed payload validation should exist for offer classes such as:

- pet creation
- effect purchase
- room decoration purchase
- trophy text
- gift purchase
- default item purchase

### Trade Flow

Trade should be treated as a room-scoped collaborative workflow.

Suggested states:

1. `Open`
2. `Offering`
3. `AcceptedByOne`
4. `AcceptedByBoth`
5. `Completing`
6. `Completed`
7. `Cancelled`
8. `Failed`

Application responsibilities:

- verify both users are present and trade-enabled
- lock offered items against concurrent mutation
- require re-acceptance after changes
- verify ownership again before completion
- move items atomically

### Pet Flow

Input shape:

- pet inventory request
- place pet
- move pet
- pick up pet

Application responsibilities:

- maintain separation between pet profile and live room actor
- validate room pet policy
- translate inventory state to room presence state

## Authorization Model

Authorization should be capability-based.

Inputs may include:

- rank-derived capabilities
- subscription-derived capabilities
- explicit grants
- explicit denies
- room-specific rights

Checks should be centralized behind policy evaluators such as:

- `CanOpenRoom`
- `CanPlaceItem`
- `CanMoveItem`
- `CanTrade`
- `CanAccessCatalogPage`
- `CanUseRoomLayout`

## Content Identity Rules

The application layer should always distinguish:

- room layout key
- room instance id
- item definition id
- item instance id
- navigator public entry id
- public-room asset package key
- catalog offer id

These identities should not be collapsed into one legacy-style table shape.

## Implementation Direction For Epsilon

The next implementation slices should follow this order:

1. request registry contracts in `Epsilon.Protocol`
2. session bootstrap application flow
3. room entry state machine
4. catalog purchase pipeline
5. capability policy evaluator
6. trade workflow model

## Bottom Line

Epsilon should be packet-compatible at the edge and application-oriented at the core.

That means:

- packets enter through protocol
- workflows run in application services
- state lives in explicit domain models
- persistence stays behind repositories

