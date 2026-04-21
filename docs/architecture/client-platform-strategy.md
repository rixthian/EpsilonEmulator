# Client Platform Strategy

Epsilon is not being built to depend on a Flash runtime.

The compatibility target is:

- protocol behavior
- hotel behavior
- content identity
- room and item semantics

The compatibility target is not:

- Adobe Flash as an execution dependency
- a browser plugin runtime
- legacy launcher coupling

## Core Rule

Epsilon should preserve the hotel contract while allowing the client platform to evolve.

That means the server must treat these concerns separately:

- compatibility protocol
- hotel application flows
- content manifests
- presentation runtime

## What Must Stay Stable

- session bootstrap rules
- navigator behavior
- room entry and room interaction rules
- item semantics
- public-room asset identity
- catalog and purchase semantics

## What Can Change

- rendering technology
- client container
- asset extraction pipeline
- launcher implementation
- transport adapters beyond the compatibility edge

## Practical Implication

Epsilon should be able to support:

- a classic compatibility adapter for historical clients
- a modern client runtime that consumes the same hotel behavior through a controlled adapter layer

The server core should not care whether presentation is delivered by:

- legacy Flash-compatible tooling
- a custom renderer
- a modern game or web runtime

## Design Consequence

Do not let Flash-era assumptions leak into:

- domain models
- repository design
- room simulation rules
- public-room asset identity
- content definitions

Those belong to Epsilon, not to the original runtime technology.
