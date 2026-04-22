# Mobile Client Connection Adaptation

Epsilon must treat iPhone, iPad, Android phones, Android tablets, and desktop as first-class launcher targets.

## Core Rule

Device connection adaptation belongs in the launcher surface, not in hotel runtime services.

## Adaptation Inputs

The launcher should adapt from:

- explicit `deviceKind` request hints
- user agent inference as fallback
- profile compatibility rules
- configurable device connection profiles

## Device Classes

- `Desktop`
- `Tablet`
- `Phone`

## Adaptation Outputs

The launcher bootstrap should return:

- selected device kind
- transport kind
- protocol family
- input mode
- touch capability flags
- reconnect policy
- heartbeat interval
- viewport constraints
- preferred asset density
- enabled connection capabilities

## Why This Is Better

It allows Epsilon to:

- serve a classic compatibility launcher to desktop/tablet where appropriate
- prefer modern touch-oriented runtimes on phones
- keep session, catalog, room, and game behavior unchanged underneath

## Implementation Direction

The launcher should expose connection-aware bootstrap data, not UI pages.

The runtime should remain portable across:

- iPhone
- iPad
- Android phones
- Android tablets
- desktop browsers
