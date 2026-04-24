# Launcher App Access

The full CMS/Launcher/Game Loader boundary is defined in [platform-boundaries.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/platform-boundaries.md). This document focuses only on the access-code handoff.

## Canonical flow

1. `User -> CMS`
   - register or login
   - reach authenticated CMS
2. `CMS -> choose access method`
   - web launcher
   - native app / launcher
3. `CMS -> issue unique launcher code`
   - short-lived
   - one-time redeem
   - tied to real hotel session
4. `Launcher app -> redeem code`
   - launcher talks to Epsilon launcher backend
   - backend returns the active session handoff
5. `Launcher app -> open client`
   - loader and client run outside the CMS
   - game starts only inside the client
6. `Emulator -> confirm presence`
   - room entry accepted is not enough
   - presence becomes real only when runtime confirms the avatar inside the room

## Rules

- The CMS never acts as the game.
- The loader never assumes the user is already inside the hotel.
- The emulator is the source of truth for entry confirmation.
- Telemetry should record:
  - code issued
  - code redeemed
  - room entry accepted
  - room presence pending
  - room presence confirmed

## Current implementation status

- CMS session and launcher handoff: implemented
- unique launcher code issue: implemented
- launcher code redeem endpoint: implemented
- launcher telemetry for issue and redeem: implemented
- native desktop launcher source: implemented
- macOS `.app` and `.dmg` packaging: implemented
- final client package: not yet implemented
- mobile launcher packaging: not yet implemented
