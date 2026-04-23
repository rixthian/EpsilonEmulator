# Launcher Surface

Epsilon needs a launcher that is adaptable across client families without moving launcher logic into the hotel runtime.

## Core Rule

The launcher is a separate application boundary.

It is responsible for:

- profile discovery
- package selection
- entry asset resolution
- locale-aware bootstrap
- session-aware client startup

It is not responsible for:

- room simulation
- catalog mutation
- gameplay commands
- hotel moderation state

## Why It Exists

Different client families need different startup contracts:

- compatibility clients need a classical bootstrap surface
- modern web runtimes need a cleaner API-oriented manifest plus a realtime `wss://` endpoint
- future native renderers may need different transport and entry rules

The launcher absorbs those differences while the hotel runtime stays stable.

## Epsilon Launcher Contract

The launcher should expose:

- profile listing
- default profile bootstrap
- explicit profile bootstrap
- localized client startup data
- session-bound bootstrap when a valid launcher session exists
- desktop launcher config for native app shells
- launcher access-code redeem and launch profile selection
- launcher telemetry for client start and handoff tracking

## Bootstrap Output

A launcher bootstrap snapshot should contain:

- client profile identity
- package manifest
- gateway base URL
- realtime gateway URL
- entry asset URL
- asset base URL
- supported interface languages
- current interface preference when authenticated
- endpoint map for the selected client family

## Transport Design

The launcher should advertise two categories of endpoint:

- control endpoints
  - bootstrap, catalog landing, diagnostics, support
- realtime endpoints
  - websocket hotel session transport

The launcher must not force clients to infer websocket URLs from HTTP URLs ad hoc.

It should emit the canonical realtime endpoint directly so client families can bind to the correct transport without transport-specific guesswork.

## Design Consequence

Launcher profiles must be configuration-driven, not hardcoded into controllers.

That keeps Epsilon flexible across:

- historical compatibility packages
- modern web runtimes
- future dedicated renderers

## Current implementation status

The launcher surface now includes:

- loader bootstrap
- one-time access-code redeem
- desktop launcher config
- update channels
- launch profile discovery and selection
- client-started telemetry

It also enforces the key rule:

- launcher acceptance is not hotel presence
- only emulator-confirmed runtime presence counts as real entry
