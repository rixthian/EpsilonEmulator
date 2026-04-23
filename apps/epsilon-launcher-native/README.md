# Epsilon Launcher Native

Base project imported from `HabboCustomLauncherBeta` and reconfigured for Epsilon.

Current behavior:

- uses the Avalonia/VB launcher shell from the original project
- redeems one-time launcher codes issued by the CMS
- reads launch profiles from the Epsilon launcher backend
- opens the published client for the selected profile
- never assumes the user is already inside the hotel

Current status:

- usable for development handoff and desktop launcher testing
- packaged as a macOS `.app` and `.dmg`
- still unstable as a product surface
- still waiting for final Unity/Nitro client targets

Current backend contract:

- `GET /launcher/desktop/config`
- `GET /launcher/update/channels`
- `GET /launcher/launch-profiles`
- `POST /launcher/access-codes/redeem`
- `POST /launcher/launch-profiles/select`
- `POST /launcher/client-started`

Local development:

```bash
cd /Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-native
dotnet build HabboCustomLauncher.sln
dotnet run --project HabboCustomLauncher.vbproj
```

Build macOS app bundle + DMG:

```bash
cd /Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-native
chmod +x package-macos.sh
./package-macos.sh
```
