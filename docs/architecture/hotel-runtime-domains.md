# Hotel Runtime Domains

This document defines the runtime surfaces that sit above raw packet handling and below delivery-specific APIs.

## Session Surface

The hotel session is broader than authentication.

It must be able to expose:

- character profile
- subscriptions
- wallet balances and recent changes
- messenger roster and pending requests
- active badges
- achievement progression
- command availability
- home-room bootstrap data

This surface is represented by `HotelSessionSnapshot`.

## Room Runtime Surface

A room runtime snapshot is not the same thing as room metadata.

It needs:

- room definition and layout
- item state
- actor runtime state
- current room activity or minigame phase
- room chat policy

This surface is represented by `RoomRuntimeSnapshot`.

## Actor Runtime Surface

Actor state must stay separate from persistent profile state.

Runtime actor state includes:

- current coordinate
- target coordinate
- body and head rotation
- carry-item state
- typing, sitting, laying, walking
- status entries used by protocol adapters

This surface is represented by `RoomActorState`.

## Wallet Surface

Wallet is its own bounded context.

It should expose:

- multi-currency balances
- recent ledger entries

Price checks, rewards, moderation grants, and progression should depend on wallet services rather than mutating profile records directly.

## Messenger Surface

Messenger must support:

- contact roster
- relationship grouping
- online presence
- pending requests

Search and conversation history can be added later without changing the core contact model.

## Badge And Achievement Surface

Badges and achievements are separate but related progression surfaces.

- badges represent visible identity and access signals
- achievements represent long-running progression and unlock milestones

These should remain explicit read models rather than hidden state on the character profile.

## Chat And Console Surface

Chat and command execution are related but distinct.

- room chat is governed by room chat policy
- commands are governed by explicit command definitions and authorization

This keeps moderation, staff tooling, and user commands out of the room simulation core.

## Minigame Surface

Minigames should be modeled as room activities.

For each room activity, Epsilon should track:

- activity kind
- current phase
- team participation
- score state

The room runtime owns this state. Protocol adapters only project it outward.
