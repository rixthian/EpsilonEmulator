# Configuration

This folder is the central configuration surface for Epsilon.

The applications still ship with local `appsettings.json` files, but they also load optional root-level overrides from this folder.

The design goal is simple:

- keep cross-service infrastructure in one place
- keep service-local behavior isolated
- keep feature policy explicit
- avoid the old emulator pattern of one giant mixed config file with unrelated settings

Load order:
1. app-local `appsettings.json`
2. app-local `appsettings.{Environment}.json`
3. `configuration/shared.json`
4. `configuration/shared.{Environment}.json`
5. `configuration/{app}.json`
6. `configuration/{app}.{Environment}.json`
7. `configuration/features.json`
8. `configuration/features.{Environment}.json`
9. environment variables with `EPSILON_` prefix

Later sources override earlier ones.

Application keys:
- `gateway`
- `launcher`
- `admin`

## Recommended files

Copy the templates you need:
- `shared.template.json` -> `shared.json`
- `gateway.template.json` -> `gateway.json`
- `launcher.template.json` -> `launcher.json`
- `admin.template.json` -> `admin.json`
- `features.template.json` -> `features.json`

## Purpose

This layout replaces the old emulator pattern of one giant mixed configuration file.

The central split is:
- `shared.json`
  auth, crypto, persistence, protocol, cross-app infrastructure
- `gateway.json`
  hotel runtime endpoint and gateway-specific behavior
- `launcher.json`
  launcher/runtime bootstrap behavior
- `admin.json`
  admin API key and admin service settings
- `features.json`
  hotel/game/bot/economy/console/network feature policy

## Operational model

Use configuration by responsibility:

- `shared.json`
  for infrastructure that must agree across services:
  auth, cryptography, persistence, protocol, Redis, Postgres, shared identifiers
- `gateway.json`
  for hotel runtime and API behavior:
  ports, protocol family, request policy, gateway-only switches
- `launcher.json`
  for client bootstrap and connection negotiation:
  launcher profiles, client families, device policies, asset roots
- `admin.json`
  for staff/admin surfaces:
  admin endpoint, admin credentials, admin-only diagnostics
- `features.json`
  for product policy:
  bots, games, moderation, economy, welcome flow, roleplay-specific future features

## Environment guidance

Recommended pattern:

- commit templates only
- keep `shared.json`, `gateway.json`, `launcher.json`, `admin.json`, and `features.json` local or environment-managed
- use environment variables for secrets and deployment overrides
- do not store production credentials in the repository

## Long-term direction

As Epsilon expands to multiple Habbo compatibility families, this folder is where the platform remains coherent.

Configuration must eventually support:

- per-environment infrastructure
- per-service behavior
- per-compatibility-family adapter selection
- per-feature enablement
- future roleplay or original gameplay modules without contaminating the core hotel baseline

## Notes

- Not every field in `features.template.json` is wired into runtime yet.
- The template includes both active and planned sections so the hotel can be configured coherently as more systems are completed.
- Do not commit live secrets.
