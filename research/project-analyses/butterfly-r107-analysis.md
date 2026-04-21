# Butterfly r107 Analysis

Source analyzed:

- [bfly-r107-source-and-database](/Users/yasminluengo/Downloads/bfly-r107-source-and-database)

## What This Artifact Adds

Butterfly r107 is one of the strongest references in the corpus for hotel-runtime behavior.

Its value is not the old implementation style. Its value is the breadth and clarity of the runtime surfaces it exposes:

- wallet and login bootstrap
- messenger and social state
- badge and achievement progression
- room-user movement and carry-item state
- room-scoped chat moderation and command handling
- minigame engines living inside room runtime
- content-driven interaction taxonomy

That makes it especially useful for translating a hotel server from a packet handler mindset into a modern application/domain model.

## High-Value Signals

### 1. Login bootstrap is richer than profile + room

`GameClient.cs` makes it clear that a real session bootstrap must prepare multiple surfaces before the user meaningfully enters the hotel:

- balances
- moderator/tooling state
- subscription badge state
- messenger initialization
- home-room and navigator state

That directly supports a dedicated `HotelSessionSnapshot` in Epsilon rather than treating bootstrap as only "character + home room".

### 2. Wallet is a first-class bounded context

Between `GameClient`, `GameClientManager`, catalog handlers, and command handlers, balances are touched constantly.

The useful translation for Epsilon is:

- wallet balances must be explicit domain data
- balance mutations should become ledgered events
- catalog, rewards, moderation grants, and minigames should depend on wallet services rather than mutating character rows directly

### 3. Messenger is not just a list of friends

The messenger code exposes separate concerns:

- buddy roster
- request queue
- search results
- relationship status
- online presence

Epsilon should therefore model messenger as its own social surface, not as a small property bag attached to `CharacterProfile`.

### 4. Room runtime needs actor state separate from profile state

`RoomUser.cs` is one of the strongest artifacts in Butterfly.

It contains runtime-only state such as:

- current tile
- target tile
- body/head rotation
- carry item
- status map
- typing
- walking
- dance/sleep/trade state
- special movement states

That strongly supports separating:

- static identity/profile
- room runtime actor state

Epsilon now needs room actors as dedicated runtime snapshots, not overloaded profile models.

### 5. Chat and console are operational systems, not string parsing hacks

The `ChatMessageHandling` area shows two separate concerns that old emulators often mixed together:

- regular room chat rules
- command/console execution

The useful lesson is to split:

- room chat policy
- command catalog
- authorization of command execution

That avoids treating every slash command as an ad hoc packet or a switch statement hidden inside room code.

### 6. Badges and achievements are independent progression surfaces

Butterfly has separate badge and achievement implementations, and both affect login/bootstrap and user-facing state.

That supports modeling:

- active badge assignments
- achievement progression

as independent read models in Epsilon.

### 7. Minigames are room-scoped engines

The room game classes show a useful shape:

- soccer
- battle banzai
- freeze

These are not global systems. They are room-scoped runtime engines with their own phase and score state.

For Epsilon, this means minigames should become:

- room activity kinds
- room activity state
- deterministic room-side orchestration

### 8. Interaction taxonomy must remain content-driven

`InteractionType.cs` is large, but it contains an important design signal: a hotel-compatible server needs a rich interaction taxonomy.

Epsilon should keep this as:

- content metadata
- feature registration
- explicit capability routing

It should not keep it as a giant procedural switch scattered through runtime code.

## Translation To Epsilon

This analysis directly supports these Epsilon surfaces:

- `WalletSnapshot`
- `MessengerContact` and `MessengerRequest`
- `BadgeAssignment`
- `AchievementProgress`
- `RoomActorState`
- `RoomChatPolicySnapshot`
- `RoomActivitySnapshot`
- `ChatCommandDefinition`

## What Not To Inherit

- giant mutable room aggregates
- transport-aware domain code
- command handlers coupled to storage
- direct balance mutation inside unrelated services
- legacy role checks scattered through runtime

## Conclusion

Butterfly r107 is a high-value runtime reference.

It is especially useful for understanding what a mature hotel session and room runtime actually need beyond login, room enter, and item placement.
