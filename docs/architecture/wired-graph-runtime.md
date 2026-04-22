# Wired Graph Runtime

Wired should be treated as a graph runtime, not as a side effect inside room handlers.

## Core Model

The useful legacy signal is simple:

- each tile or item node can hold state
- links define possible signal transfer
- state changes propagate through connected nodes
- update results are a set of affected positions or entities

That translates into Epsilon as:

- graph-backed wired state per room
- explicit propagation service
- deterministic evaluation order
- room-scoped runtime isolation

## What The Runtime Needs

The minimum useful components are:

- `WiredNodeState`
- `WiredEdgeState`
- `WiredPropagationService`
- `WiredRoomSnapshot`
- `WiredMutationResult`

The service should answer:

- which nodes changed
- which room effects changed
- whether propagation reached a stable state

## Operational Rules

Wired execution must be:

- room-local
- deterministic
- bounded
- inspectable

That means:

- no hotel-wide locks
- no hidden side effects in chat or movement handlers
- no unbounded recursive propagation
- explicit diagnostics for affected nodes

## Why This Matters

A graph runtime gives Epsilon the correct base for:

- switches
- gates
- teleport chains
- triggers
- conditions
- effects
- future automation logic

The old code is not worth importing.
The separation of concern is.
