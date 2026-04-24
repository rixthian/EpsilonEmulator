# Nitro Imager Reference

This source is treated as an architecture reference for server-side avatar imaging. It is not a CMS, launcher, emulator, or game client. Do not wire it directly into production runtime without an Epsilon-owned normalization, security, caching, and URL policy layer.

## Source

| Field | Value |
| --- | --- |
| Requested URL | `git@github.com:billsonnn/nitro-imager.git` |
| Working clone URL | `https://github.com/billsonnn/nitro-imager.git` |
| Commit | `def234e058589191f6556daf268555ad3128e84e` |
| Local reference path | `/Users/yasminluengo/Documents/Playground/reference-sources/nitro-imager` |
| Package language | TypeScript / Node.js |
| HTTP runtime | Express |
| Rendering runtime | `node-canvas`, `gifencoder` |
| Asset container focus | `.nitro` bundles |
| Package license metadata | `ISC` in `package.json`; no standalone license file observed in the repository root. |
| Reference policy | `architecture_reference_no_direct_runtime_dependency` |

The SSH endpoint failed locally because GitHub host-key verification is not configured in this environment. The reference clone was made through HTTPS.

## Why This Matters

`nitro-imager` is useful because it demonstrates a clean separation between:

- avatar figure metadata
- figure-to-library resolution
- `.nitro` asset bundle loading
- server-side canvas composition
- render-result caching
- HTTP image delivery

This is exactly the separation Epsilon needs for account avatars, CMS profile previews, launcher user cards, friend list portraits, room user thumbnails, and admin/moderation identity views.

## Observed Structure

| Area | Evidence | Epsilon Interpretation |
| --- | --- | --- |
| Application shell | `src/main.ts`, `src/app/Application.ts` | A small process initializes core services, avatar renderer, and HTTP routing. |
| HTTP router | `src/app/router/habbo-imaging/**` | Public image endpoint with query parsing. Epsilon should use neutral route names such as `/media/avatar/render`. |
| Avatar renderer | `src/app/avatar/AvatarRenderManager.ts`, `AvatarImage.ts`, `AvatarStructure.ts` | Avatar rendering is its own subsystem, not part of CMS, launcher, or room runtime. |
| Figure data | `FigureDataContainer.ts`, `interfaces/figuredata/**` | Figure composition is data-driven and should map to Epsilon figure-data manifests. |
| Figure map | `AvatarAssetDownloadManager.ts` | Figure parts resolve to asset libraries before rendering. |
| Effects | `EffectAssetDownloadManager.ts` | Effects are separate render overlays and should be entitlement-checked by Epsilon before use in user-facing surfaces. |
| Asset bundles | `src/core/asset/NitroBundle.ts`, `AssetManager.ts` | `.nitro` bundles contain compressed JSON plus PNG texture data loaded into memory. |
| Image output | `node-canvas`, `gifencoder` | PNG and GIF outputs are generated server-side, then cached to disk. |
| Config | `.env.new`, `config.json` | Runtime depends on explicit URLs for actions, figuredata, figuremap, effectmap, and asset bundle templates. |

## Runtime Flow Observed

1. HTTP request provides a figure string and render options.
2. Query utilities parse direction, head direction, gesture, posture/action, effect, dance, size, frame, and image format.
3. The service builds a deterministic cache filename from render options.
4. If a cached image exists, it returns the file immediately.
5. If an effect is requested, the effect library is downloaded if missing.
6. The renderer validates or completes the figure with mandatory parts.
7. The figure map resolves avatar part libraries.
8. Missing `.nitro` libraries are downloaded and loaded into memory.
9. Canvas composition renders the avatar and auxiliary sprites.
10. PNG is returned directly, or GIF frames are encoded and saved.

## Epsilon Design Decisions

| Decision | Rule |
| --- | --- |
| Service boundary | Build avatar imaging as a separate service or module. Do not place it in the CMS frontend or launcher app. |
| Route naming | Use Epsilon-owned neutral APIs, not brand-specific route names. |
| Source of truth | Account/avatar service owns selected figure data. Imaging service only renders requested, validated presentation. |
| Asset source | Imaging must consume normalized Epsilon manifests and signed asset URLs, not arbitrary external URLs. |
| Cache policy | Cache key must include figure, action, direction, size, effect, frame, image format, asset manifest version, and renderer version. |
| Security | Validate figure strings, action values, effects, size, and output format before rendering. Never allow arbitrary path or URL injection. |
| Entitlements | Client may request an effect or wearable preview, but backend must validate ownership/permission when the image represents a real user. |
| Runtime coupling | The game client can use the same rendered thumbnails, but room avatar rendering remains a client/runtime concern. |

## Recommended Epsilon Service Shape

| API | Purpose |
| --- | --- |
| `GET /media/avatar/render` | Render a validated avatar from explicit render options. |
| `GET /media/avatar/user/{characterId}` | Render a user's current avatar using account/avatar service data. |
| `POST /internal/media/avatar/prewarm` | Prewarm common avatar cache entries after figure changes. |
| `DELETE /internal/media/avatar/cache/{characterId}` | Invalidate avatar image cache after figure or entitlement changes. |

Minimum render options:

- `figure`
- `direction`
- `headDirection`
- `gesture`
- `action`
- `effect`
- `size`
- `headOnly`
- `frame`
- `format`

## What To Preserve

- Data-driven avatar composition.
- Separate figuredata and figuremap responsibilities.
- Lazy loading of needed asset libraries.
- Deterministic output cache keys.
- Server-side generation for CMS/launcher/social surfaces.
- Independent process boundary for heavy image rendering.

## What To Redesign

- Replace brand-specific endpoint names with Epsilon-owned API names.
- Replace unbounded external URL config with signed asset manifests.
- Add request validation, rate limiting, and cache quotas.
- Add structured metrics for render latency, cache hits, cache misses, bundle download failures, invalid figure strings, and renderer exceptions.
- Add a compatibility adapter for imported `.nitro` bundles only after provenance and licensing are clear.
- Keep gameplay movement/avatar state out of this service.

## Integration With Current Epsilon Work

| Epsilon Area | How `nitro-imager` Helps |
| --- | --- |
| Figure Data Catalog | Confirms `figuredata` must remain a first-class manifest. |
| Avatar Asset Catalog | Confirms figure libraries/effects need separate bundle indexing. |
| Visual Asset Pipeline | Adds server-side image generation as a separate layer after asset inventory. |
| CMS | Should call imaging APIs for profile/user preview images. |
| Launcher | Should call imaging APIs for authenticated account/avatar identity display. |
| Runtime Gateway | Should not delegate authoritative avatar state to the imaging service. |
| Moderation/Admin | Can use imaging APIs to render user identity snapshots in reports and sanctions. |

## Open Implementation Tasks

1. Add an Epsilon avatar-imaging service specification.
2. Define a neutral avatar render request/response contract.
3. Define cache-key and invalidation rules.
4. Decide whether MVP uses Node.js/TypeScript with `node-canvas` or a .NET image pipeline.
5. Add tests for invalid figure strings, oversized requests, forbidden effects, and cache hit behavior.
6. Connect the service only to normalized Epsilon figure/avatar manifests.
