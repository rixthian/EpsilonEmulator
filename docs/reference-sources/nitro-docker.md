# Nitro Docker Reference

This source is treated as an orchestration, endpoint, asset-service, and local runtime topology reference for Epsilon. It must not be copied into Epsilon as runtime code without an explicit license decision.

The important lesson is structural: a working hotel stack is not one CMS page. It is a separated service topology where CMS, client, emulator/runtime, assets, imaging, database, backups, and reverse proxy concerns are isolated.

## Source

| Field | Value |
| --- | --- |
| Requested URL | `git@github.com:Gurkengewuerz/nitro-docker.git` |
| Working clone URL | `https://github.com/Gurkengewuerz/nitro-docker.git` |
| Commit | `aa5826938aa1f9f1660d75f0163e8af1f2fe5dcb` |
| Local reference path | `/Users/yasminluengo/Documents/Playground/reference-sources/nitro-docker` |
| Primary purpose | Docker topology for Nitro client, Arcturus Community emulator, assets, imaging, CMS, database, and backup services |
| Declared license | AGPL-3.0 |
| Reference policy | `architecture_reference_no_direct_code_copy` |

## Observed Service Topology

| Service | Observed Role | Exposed Local Port | Depends On | Epsilon Interpretation |
| --- | --- | ---: | --- | --- |
| `arcturus` | Emulator/runtime process with WebSocket gateway | `2096` | `db` | Maps conceptually to `Epsilon.Gateway` plus `Epsilon.CoreGame` and `Epsilon.Rooms`. |
| `nitro` | Static web client served by nginx | `3000` | config files | Maps to Epsilon's future packaged game client or loader asset surface, not to the CMS. |
| `db` | MySQL 8 durable store | `3310` | `backup` | Epsilon remains PostgreSQL-first, but the stack confirms DB must be an explicit first-class service. |
| `backup` | Scheduled DB backup sidecar | none | `db` | Epsilon needs local and production backup jobs from the beginning of persistent testing. |
| `assets` | Static asset CDN-like nginx surface | `8080` | `imager`, `imgproxy` | Maps to an Epsilon asset service/CDN surface. |
| `imager` | Avatar imaging/render service | internal | `assets` | Maps to the Epsilon avatar imaging service spec. |
| `imgproxy` | Remote image proxy/cache | internal | asset cache volume | Useful only with strict allowlists and cache policy. |
| `cms` | AtomCMS public portal | `8081` | `assets`, `arcturus` | Maps to Epsilon CMS, but Epsilon must not couple CMS directly to runtime internals. |

## Endpoint Lessons

| Endpoint Family | Nitro Docker Example | Epsilon Rule |
| --- | --- | --- |
| Realtime gameplay | `ws://127.0.0.1:2096` | Epsilon local dev may use loopback insecure transport, but production gameplay must use `wss://`. |
| Static client | `http://127.0.0.1:3000` | Static client delivery is separate from CMS. |
| Static assets | `http://127.0.0.1:8080/assets` | Assets should be versioned, cacheable, and independent from account UI. |
| CMS | `http://127.0.0.1:8081` | CMS is a portal and account/community surface, not the game runtime. |
| Database | `127.0.0.1:3310` | DB should be reachable for local development but not exposed in production. |

## Configuration Surfaces Observed

| Config Surface | Purpose | Epsilon Translation |
| --- | --- | --- |
| `.env` | Shared service ports, DB credentials, emulator host, RCON, imaging URLs | Split into typed Epsilon service config files and secrets. |
| `.cms.env` | CMS-specific application settings, DB, RCON, captcha, payment, client path | Keep CMS config separate from launcher and runtime config. Do not reuse sample secrets. |
| `nitro/renderer-config.json` | Client renderer URLs for socket, assets, gamedata, figures, furniture, effects, badges | Epsilon needs a launch-profile manifest consumed by the loader/client. |
| `nitro/ui-config.json` | Client UI behavior, CMS links, room models, currencies, catalog UI details | Epsilon should keep UI config separate from runtime authority. |
| `assets/configuration.json` | Asset conversion inputs and output flags | Epsilon importers should normalize legally usable assets into canonical manifests. |
| `compose.traefik.yaml` | Reverse proxy host rules | Epsilon should support local direct ports and production reverse proxy profiles. |

## Renderer Config Lessons

The renderer config is especially useful because it makes the game client boot contract explicit:

- `socket.url` points to realtime transport.
- `asset.url` points to a static asset root.
- `external.texts.url`, `furnidata.url`, `productdata.url`, `avatar.figuredata.url`, and related entries point to content manifests.
- `avatar.asset.url`, `furni.asset.url`, `pet.asset.url`, `generic.asset.url`, and badge/effect URLs define versioned content resolution.
- performance and connection behavior such as FPS and pong interval are client runtime configuration, not CMS behavior.

Epsilon should translate this into a neutral launch profile:

| Nitro Docker Concept | Epsilon Launch Profile Field |
| --- | --- |
| `socket.url` | `runtime.realtimeUrl` |
| `asset.url` | `assets.baseUrl` |
| `external.texts.url` | `content.localizationUrls` |
| `furnidata.url` | `content.itemDefinitionsUrl` |
| `productdata.url` | `content.productDefinitionsUrl` |
| `avatar.figuredata.url` | `content.avatarFigureDataUrl` |
| `avatar.figuremap.url` | `content.avatarAssetMapUrl` |
| `furni.asset.url` | `assets.furnitureBundleTemplate` |
| `badge.asset.url` | `assets.badgeTemplate` |
| `system.pong.interval.ms` | `runtime.heartbeatIntervalMs` |

## Asset Pipeline Lessons

Nitro Docker treats assets as their own service boundary. That is the correct direction for Epsilon.

Observed responsibilities:

- static asset hosting through nginx
- conversion from source asset metadata into runtime JSON and bundles
- avatar imaging as a separate HTTP service
- image proxy/cache as a separate concern
- CORS and cache headers at the asset edge

Epsilon rules:

1. The CMS may display assets, but does not own the asset pipeline.
2. The launcher may download assets, but does not decide gameplay ownership.
3. The loader/client may render assets, but gameplay authority remains server-side.
4. The asset service should expose versioned manifests and content hashes.
5. Asset provenance and licensing must be tracked before import.

## Security And License Risks

| Risk | Why It Matters | Epsilon Decision |
| --- | --- | --- |
| AGPL-3.0 license | Direct code copying may impose AGPL obligations on the Epsilon distribution. | Use as reference unless an explicit license decision is made. |
| Example secrets | `.cms.env` includes sample app keys and credentials. | Never reuse sample secrets. Generate per-environment secrets. |
| Public direct DB port | Useful locally but unsafe in production. | Local only. Production DB remains private. |
| Emulator/CMS coupling | CMS can become tightly coupled to emulator DB/RCON. | Epsilon CMS talks through typed APIs and handoff contracts. |
| Public CORS on assets | Convenient for development but can be too broad. | Restrict origins for production. |
| Asset provenance | Source assets may have rights constraints. | Import only owned, licensed, or explicitly allowed assets. |
| Untyped JSON config drift | Client config can diverge from backend contracts. | Validate launch profiles with schemas and contract tests. |

## Epsilon Translation Map

| Nitro Docker Service | Epsilon Equivalent | Translation Rule |
| --- | --- | --- |
| `cms` | `cms/system` | Account/community/access-code portal only. No gameplay state. |
| `nitro` | `src/Epsilon.Launcher/Assets/clients/*` and future Unity/native client package | Client package delivered by launcher/profile, not CMS. |
| `arcturus` | `Epsilon.Gateway`, `Epsilon.CoreGame`, `Epsilon.Rooms` | Runtime authority and realtime command execution. |
| `assets` | Future `Epsilon.Assets` or static CDN profile | Versioned content delivery and immutable asset manifests. |
| `imager` | Avatar imaging service/module | Render avatar previews for CMS/launcher/social/admin. |
| `imgproxy` | Optional media proxy | Use only with allowlists, quotas, and cache controls. |
| `db` | PostgreSQL service | Durable source of truth. Avoid MySQL compatibility coupling unless importing. |
| `backup` | DB backup job | Required once persistent alpha testing starts. |
| Traefik profile | Reverse proxy/infrastructure profile | Keep direct local ports and proxy deployment separate. |

## Architecture Conclusions

1. The local stack must be service-oriented even if the backend remains a modular monolith.
2. CMS, launcher, loader/client, runtime, assets, imaging, and persistence must be separate deployment concepts.
3. Launch profiles are the correct place to connect a client package to runtime and asset endpoints.
4. The CMS should issue launcher access, not render or simulate the hotel.
5. The launcher should open the loader/client, not invent room state.
6. The runtime gateway is the only authority for room presence, chat, movement, item placement, and gameplay actions.
7. Asset conversion/import is a build-time or tooling concern, not a runtime gameplay concern.

## Next Engineering Actions

1. Add an Epsilon local orchestration topology that mirrors the useful separation without copying the stack.
2. Convert current launcher bootstrap output into a formal launch-profile schema.
3. Add asset service endpoints for versioned manifests and immutable file URLs.
4. Add contract tests that verify CMS, launcher, and loader cannot mark presence before runtime confirmation.
5. Add persistent DB and Redis compose profiles when the in-memory slices are reduced.
