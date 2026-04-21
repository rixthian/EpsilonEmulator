# ADR 0001: Use A Modern Runtime With Legacy Compatibility Adapters

## Status

Accepted

## Context

Historical emulator projects are valuable references, but most of them suffer from the same issues:

- transport, business logic, and SQL are tightly coupled
- weak testing strategy
- fragile threading models
- security debt in auth and web layers
- poor version separation

Epsilon Emulator needs long-term maintainability without losing protocol fidelity.

## Decision

Use a modern `.NET 10` codebase with explicit module boundaries and versioned compatibility adapters.

## Consequences

Benefits:

- strong language/runtime support
- clean async networking options
- mature web and DI stack
- good test tooling
- straightforward path to observability and ops

Costs:

- no direct compatibility with legacy code layouts
- more up-front specification work
- requires disciplined documentation to avoid folklore-driven behavior
