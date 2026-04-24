# Desktop Launcher Spec

The full modular boundary between CMS, Launcher App, Game Loader, Launcher Backend, Runtime Gateway, and Emulator is defined in [platform-boundaries.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/platform-boundaries.md).

## Scope

This document defines the `desktop launcher` contract for Epsilon.

The launcher is a separate application from the CMS and from the hotel runtime.

It is responsible for:

- redeeming access codes
- selecting a launch profile
- updating launcher-managed client packages
- starting the correct client
- reporting launcher telemetry

It is not responsible for:

- simulating rooms
- confirming room entry
- deciding that the user is already inside the hotel

Only the emulator can confirm real presence inside the hotel.

## Canonical Entry Flow

1. `User -> CMS`
   - register or login
   - reach authenticated CMS home
2. `CMS -> access choice`
   - web launcher
   - native launcher app
3. `CMS -> issue launcher code`
   - short-lived
   - one-time redeem
   - bound to a real hotel session
4. `Launcher app -> redeem code`
   - obtains session handoff
   - resolves available launch profiles
5. `Launcher app -> select profile`
   - desktop Unity
   - vendor client
   - compatibility client
6. `Launcher app -> update package if needed`
7. `Launcher app -> start client`
8. `Client -> connect to emulator`
9. `Emulator -> confirm presence`

## Desktop Launcher App

Recommended shape:

- shell app
  - native window
  - sign-in handoff / redeem view
  - update progress
  - launch profile selector
  - settings
- package manager
  - checks manifest
  - downloads package diffs or full package
  - verifies hashes
- launch runner
  - starts the selected client
  - passes session handoff
  - watches process lifecycle
- telemetry sender
  - sends launcher events
  - never fakes hotel presence

Recommended stacks:

- desktop app:
  - `Electron`
  - or `Tauri`
  - or `.NET` desktop shell
- package/update:
  - manifest + hash validation
- client handoff:
  - session ticket or redeem token

## Local Config

The launcher app needs local machine config, separate from server config.

Suggested local config file:

- macOS:
  - `~/Library/Application Support/EpsilonLauncher/config.json`
- Windows:
  - `%AppData%/EpsilonLauncher/config.json`
- Linux:
  - `~/.config/EpsilonLauncher/config.json`

Suggested fields:

```json
{
  "launcherId": "desktop-main",
  "platform": "macos",
  "locale": "es-ES",
  "hotelBaseUrl": "http://127.0.0.1:8081",
  "launcherApiBaseUrl": "http://127.0.0.1:5001",
  "defaultChannel": "stable",
  "defaultProfileKey": "unity-desktop",
  "installRoot": "/Users/example/Games/EpsilonHotel",
  "cacheRoot": "/Users/example/Library/Caches/EpsilonLauncher",
  "logRoot": "/Users/example/Library/Logs/EpsilonLauncher",
  "autoLaunchOnRedeem": true,
  "closeCmsOnLaunch": false,
  "hardwareAcceleration": true,
  "telemetryEnabled": true,
  "rememberLastProfile": true
}
```

Rules:

- local config is machine-scoped
- it never defines hotel authority
- it never marks the user as inside the hotel
- it only controls launcher-side behavior

## Redeem Code Flow

Current Epsilon endpoints:

- `POST /launcher/access-codes`
- `GET /launcher/access-codes/current`
- `POST /launcher/access-codes/redeem`

Canonical redeem flow:

1. CMS issues code
2. launcher app asks user for the code or receives deep link
3. launcher app calls `POST /launcher/access-codes/redeem`
4. launcher receives:
   - session handoff
   - launcher URL
   - entry asset URL when applicable
   - selected/default profile metadata
5. launcher chooses profile
6. launcher starts client

Required telemetry:

- `launcher_code_issued`
- `launcher_code_redeemed`
- `launcher_profile_selected`
- `launcher_update_started`
- `launcher_update_completed`
- `launcher_client_started`
- `room_presence_pending`
- `room_presence_confirmed`

Critical rule:

- `redeemed` does not mean `inside the hotel`
- `client started` does not mean `inside the hotel`
- only runtime-confirmed presence means `inside the hotel`

## Update Channel

The launcher needs update channels for both:

- launcher app itself
- client packages

Minimum channels:

- `stable`
- `beta`
- `canary`
- `internal`

Channel model:

```json
{
  "channelKey": "stable",
  "displayName": "Stable",
  "launcherManifestUrl": "https://launcher.example.com/stable/launcher.json",
  "packageManifestUrl": "https://launcher.example.com/stable/packages.json",
  "allowDowngrade": false,
  "requiresSignedPackages": true
}
```

Rules:

- launcher binary updates and client updates are separate concerns
- package manifests must include hashes
- the launcher must not start partially updated packages

## Launch Profile

A `launch profile` defines which client the launcher should start.

Suggested fields:

```json
{
  "profileKey": "unity-desktop",
  "displayName": "Unity Desktop",
  "channel": "stable",
  "platforms": ["windows", "macos", "linux"],
  "clientKind": "unity",
  "packageKey": "unity_desktop_main",
  "entryExecutable": "EpsilonHotel.app",
  "arguments": [
    "--launcher",
    "--redeem-ticket={ticket}"
  ],
  "requiresRedeemCode": true,
  "supportsSafeReconnect": true,
  "supportsOverlayTelemetry": true,
  "isDefault": true
}
```

Suggested first profiles for Epsilon:

- `unity-desktop`
- `vendor-client`
- `game-loader`
- `flash-compat`

Rules:

- the launcher may know multiple profiles
- only one profile is selected per launch
- profile selection is launcher-side only
- hotel runtime does not trust profile claims blindly

## Current API Contract

The desktop launcher contract now exists in Epsilon:

- `GET /launcher/desktop/config`
- `GET /launcher/update/channels`
- `GET /launcher/update/channel/{channelKey}`
- `GET /launcher/launch-profiles`
- `POST /launcher/launch-profiles/select`
- `POST /launcher/client-started`

## Implementation Order

Completed:

1. launcher desktop contract
2. redeem screen
3. channel manifest format
4. launch profile manifest
5. native launcher shell

Still pending:

1. deeper desktop package/update flow
2. final desktop client integration
3. release signing/notarization hardening
