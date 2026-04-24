# CMS Runtime Base Adoption

This document defines how Epsilon temporarily uses a validated CMS runtime stack as the local CMS/assets/database base.

This is an adoption bridge, not the final Epsilon architecture. The goal is to have a real working base while Epsilon continues to build its own launcher, runtime gateway, and original systems.

## Adopted Components

| Component | Adopted Source | Local Role In Epsilon | Git Policy |
| --- | --- | --- | --- |
| Adopted CMS runtime | CMS image build | Temporary public CMS base | Local runtime only |
| Assets service | Assets service | Temporary static asset and manifest server | Local runtime only |
| Avatar imager | Imager service | Temporary avatar preview renderer | Local runtime only |
| MySQL schema/data | Exported from running CMS DB | Temporary CMS/emulator database bootstrap | SQL dump in ignored runtime dir |
| Game client | Client surface | Temporary client reference surface | Local runtime only |
| Runtime service | Runtime image build | Temporary emulator/runtime reference | Local runtime only |

## Local Paths

| Path | Purpose |
| --- | --- |
| External CMS runtime source path | Working reference clone used for validation. Pass this path to `tools/cms_runtime_adopt.sh`. |
| `/Users/yasminluengo/Documents/Playground/EpsilonEmulator/vendor/cms-runtime-base` | Epsilon-owned local runtime copy, ignored by git. |
| `/Users/yasminluengo/Documents/Playground/EpsilonEmulator/vendor/cms-runtime-base/db/dumps/epsilon_cms_runtime.sql.gz` | Portable DB bootstrap dump. |

## Tooling

| Script | Purpose |
| --- | --- |
| `tools/cms_runtime_adopt.sh` | Copies the validated CMS/assets/runtime base into `vendor/cms-runtime-base` and exports DB as a dump. |
| `tools/cms_runtime_start.sh` | Starts the adopted CMS runtime stack from `vendor/cms-runtime-base`. |
| `tools/cms_runtime_check.sh` | Verifies CMS, client, assets, imager, and realtime port health. |

## Operating Rule

The adopted stack may provide the temporary CMS/assets/database base, but Epsilon still keeps strict product boundaries:

- CMS handles account/community portal behavior.
- Launcher handles access-code redemption and boot profile handoff.
- Loader/client runs the game surface.
- Runtime/emulator confirms actual hotel presence.
- Assets/imager serve presentation content.
- Database persists server-side state.

The CMS must not claim that the player is inside the hotel. Only runtime room join confirmation can set that state.

## License And Provenance

The upstream stack declares AGPL-3.0. Treat the adopted runtime copy as local development infrastructure unless the project intentionally accepts the license consequences.

Assets are provenance-sensitive. They are acceptable for local study and compatibility testing only when rights are confirmed. Epsilon should still move toward owned, licensed, or original assets for production.

## Current Temporary Endpoints

| Endpoint | Purpose |
| --- | --- |
| `http://127.0.0.1:8081/` | Temporary CMS |
| `http://127.0.0.1:3000/` | Temporary client surface |
| `http://127.0.0.1:8080/` | Temporary assets/imager surface |
| `ws://127.0.0.1:2096` | Temporary realtime transport |
| `127.0.0.1:3310` | Temporary MySQL database |

## Migration Path Back To Epsilon-Native

1. Keep using the adopted CMS only as a baseline for registration/login/content behavior.
2. Build Epsilon launcher handoff against the temporary CMS and runtime endpoints.
3. Replace direct CMS-to-runtime assumptions with Epsilon launch-profile contracts.
4. Replace MySQL and adopted-CMS dependencies with Epsilon persistence once the flow is stable.
5. Replace provenance-sensitive assets with licensed or original content bundles.
