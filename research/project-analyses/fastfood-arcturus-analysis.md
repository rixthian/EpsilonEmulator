# fastfood_arcturus Analysis

Source analyzed:

- [fastfood_arcturus](/Users/yasminluengo/Downloads/fastfood_arcturus)

## What This Artifact Is

This package is not a full hotel emulator.

It is a game plugin for Arcturus, centered on a separate fast-food style game flow and an external API bridge.

The zip contains:

- plugin source code
- plugin packet registrations
- game-specific incoming and outgoing packets
- a manager for user game state
- API connection code
- game assets such as `BaseJump.swf` and `BasicAssets.swf`
- plugin configuration and SQL

## Why It Matters

This artifact is useful for Epsilon in a narrow but important way.

It shows how a hotel-compatible environment can host a specialized game subsystem without collapsing that logic into the main hotel runtime.

That is directly relevant to Epsilon's long-term design.

## Useful Signals

### 1. Feature-specific packet surfaces

The plugin registers its own packet handlers for game actions such as:

- get games
- get account game status
- load game
- quit game

This reinforces the idea that Epsilon should model specialized feature flows as bounded protocol surfaces, not as random additions to core room handlers.

### 2. Game logic sits behind a dedicated manager

`FastFoodManager` manages per-user game state separately from the rest of the hotel.

That is a good design signal for Epsilon:

- minigame state should live in explicit bounded services
- user game state should not leak into general room or auth code

### 3. External service integration is isolated

The plugin authenticates the user against an external API before loading the game.

Even though the implementation is legacy, the architectural lesson is useful:

- specialized subsystems can have their own service integrations
- hotel identity can be translated into subsystem-specific session tokens

### 4. Game launch is an application flow

`LoadGameIncomingPacket` does not just open a room.

It performs:

- game selection
- API authentication
- conditional load
- queue leave or game open responses
- session state update

That is another strong signal that Epsilon should treat large interactions as flow orchestration, not isolated packet reactions.

## What Epsilon Should Not Take

- Arcturus plugin APIs
- Flash-based game runtime assumptions
- Java plugin packaging
- direct database access inside feature managers
- packet numbers as business logic

## What Epsilon Should Take

- feature-specific protocol modules
- dedicated manager/service per subsystem
- isolated external integration boundaries
- explicit game-launch flows

## Bottom Line

`fastfood_arcturus` does not change Epsilon's core hotel architecture.

Its value is architectural reinforcement:

- complex subsystems should be modular
- protocol surfaces should be bounded
- external game services should be isolated

It is a useful signal for future minigame design, not a core reference for room entry or hotel persistence.

