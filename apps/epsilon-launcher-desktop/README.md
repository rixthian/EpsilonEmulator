# Epsilon Launcher Desktop

Desktop shell for the Epsilon launcher flow.

Current scope:

- reads launcher desktop config from `http://127.0.0.1:5001`
- stores machine-local config in Electron `userData`
- redeems one-time launcher codes issued by the CMS
- selects a launch profile
- opens the published hotel client in a dedicated desktop window
- reports `launcher_client_started`

This app does not decide that the user is already inside the hotel.
Only the emulator confirms real presence.

Status:

- reference shell only
- useful for launcher-contract testing
- not the primary desktop launcher release

## Run

```bash
cd /Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-desktop
npm install
npm start
```

## Backend prerequisites

- CMS running on `http://127.0.0.1:4100`
- launcher backend running on `http://127.0.0.1:5001`
- gateway running on `http://127.0.0.1:5100`
