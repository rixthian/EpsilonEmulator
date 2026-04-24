# Project Requirements

Date: 2026-04-23

This document defines the local SDK and runtime requirements for Epsilon. It exists so the CMS, launcher, popup/loader, Unity client, and emulator are not treated as the same application.

## Required Local SDKs

Unity installation is managed through Unity Hub. Unity's current Hub documentation states that Hub manages Editor versions and modules, and its CLI can install editors/modules in headless mode.

| Requirement | Required For | Current Target | Local Status On 2026-04-23 |
| --- | --- | --- | --- |
| .NET SDK | Emulator, gateway, launcher backend, admin APIs, tests | `10.0.202` currently installed locally | Installed |
| Node.js | CMS, Electron launcher shell, tooling | `v25.9.0` currently installed locally | Installed |
| npm | JS package management | `11.12.1` currently installed locally | Installed |
| Unity Hub | Unity editor installation and module management | `3.17.3` via Homebrew cask | Installed |
| Unity Editor | Future Unity client/loader package | `6000.3.14f1` Apple silicon target | Install attempted through Unity Hub CLI |
| Unity WebGL module | WebGL/WebAssembly loader builds | `webgl` module | Install attempted with Unity editor |
| Unity macOS IL2CPP module | macOS desktop client builds | `mac-il2cpp` module | Install attempted with Unity editor |

## Component Separation Requirement

| Component | Owns | Does Not Own |
| --- | --- | --- |
| CMS | Registration, login, community pages, launcher access code display/copy. | Game runtime, room simulation, loader execution, presence confirmation. |
| Launcher app | Local config, update channel, code redemption, profile selection, internal popup/window that launches the game loader. | CMS pages, room simulation, item ownership, economy, authoritative presence. |
| Launcher popup/window | Transitional launcher-owned UI that starts or embeds the game loader. | Account registration, catalog mutation, room authority. |
| Game loader | Ticket validation, asset boot, Unity/WebGL/native client startup, runtime gateway connection. | Web account login, CMS community screens, economy authority. |
| Emulator/runtime gateway | Authoritative room entry, presence, movement, chat, item placement, and gameplay state. | CMS UX, launcher updates, client-only claims. |

## Required Access Flow

```text
User
  -> CMS
  -> register/login
  -> CMS shows one unique launcher code
  -> Launcher App redeems code
  -> Launcher App opens internal popup/window
  -> popup/window starts Game Loader
  -> Game Loader validates ticket
  -> Game Loader connects to Runtime Gateway
  -> Emulator confirms presence
  -> only then user is inside the hotel
```

The user is not inside the game at CMS login time. The user is not inside the game when the launcher redeems a code. The user is not inside the game when the popup opens. Presence becomes true only after the emulator confirms the avatar inside runtime state.

## Launcher Popup Rule

The popup belongs to the launcher layer, not to the CMS.

The popup must do one job: bridge from launcher state to the internal game loader. It may show loading, update, retry, and error states. It must not become a CMS page or show community/account content.

Correct behavior:

1. Launcher redeems the code.
2. Launcher selects a launch profile.
3. Launcher opens an internal popup/window.
4. Popup/window loads the game loader URL or embedded client package.
5. Loader validates the ticket and starts runtime connection.

Incorrect behavior:

- CMS opens the game directly as if the user were already inside.
- Launcher claims room presence after code redemption.
- Popup contains CMS login/register/community UI.
- Loader shows debug bootstrap JSON as player-facing experience.
- Client trusts CMS data for ownership, wallet, trading, or room authority.

## Unity Client Requirement

Unity is a future client/runtime package target. It should be integrated only after the launcher contract is stable.

Minimum Unity client responsibilities:

- accept a launcher-issued ticket through command-line argument, deep link, or embedded launch URL
- validate that ticket with the launcher backend
- download or load versioned assets through Addressables or equivalent package manifests
- connect to the runtime gateway
- wait for emulator-confirmed presence
- render gameplay only after authoritative state exists
- report crash, boot, loading, and connection telemetry

Unity must not own accounts, item grants, wallet balances, purchases, trades, sanctions, or moderation authority.

## Verification Command

Run this command from the repository root:

```bash
python3 tools/check_project_requirements.py
```

The check reports local SDK presence and flags whether Unity Hub, Unity editor, WebGL support, and macOS IL2CPP support are visible.

## External References

- [Unity Hub overview](https://docs.unity.com/en-us/hub)
- [Unity Hub CLI reference](https://docs.unity.com/en-us/hub/hub-cli)
- [Download and manage Unity Editor installations](https://docs.unity.com/en-us/hub/install-editors)
