# V28 DCR Digital Archaeology Report

Source artifact: `/Users/yasminluengo/Downloads/v28dcr`

Date inspected: 2026-04-23

Purpose: preserve technical knowledge from a legacy Shockwave-era virtual world client pack without redistributing copyrighted assets, cloning proprietary behavior, or depending on private protocols.

## 1. Executive Findings

The inspected directory is a compact legacy Director/Shockwave client asset pack, not a modern web client. The strongest evidence is the dominant `XFIR` / `RIFX` signatures in `.dcr` and `.cct` files, the presence of `habbo.dcr`, many `hh_*.cct` cast libraries, `figuredata.xml`, `draworder.xml`, `partsets.xml`, `animation.xml`, `furnidata`, `productdata`, external text/variable files, and large sets of furniture and badge media.

The pack contains 2,382 files and approximately 42.15 MB of file payload. It is weighted heavily toward content libraries and media:

- 2,131 `.cct` files.
- 1 `.dcr` bootstrap container.
- 234 `.gif` badge images.
- XML/text configuration for avatar assembly, draw order, animation, localization, and runtime variables.
- 1,929 files inside `furni/`, mostly furniture-related cast libraries and sound-machine samples.

Direct evidence indicates this is a V28-era pack. The `about dcr.txt` file describes it as "Fresh V28 Dcr's with English & Norwegian texts". Several placeholder `.cct` files are actually Apache-style `404 Not Found` HTML documents referencing a path containing `r28_20081202_1744_10717_6c80a6dd09d60c84a1396e0ceb63e445`. This is strong evidence for a late-2008 release lineage, but not cryptographic proof of original provenance.

The artifact should be treated as a cultural/technical reference source, not a production dependency. The correct engineering use is to extract concepts: modular cast loading, avatar part taxonomy, room/public-space library separation, text-driven UI labels, manifest-driven content lookup, furniture metadata, and patch/localization layering. The incorrect use would be copying assets, reproducing proprietary packet behavior, or building a product around redistributed legacy media.

## 2. Methodology and Evidence Policy

This pass used non-destructive filesystem analysis only:

- File inventory with `find`, `file`, size checks, and extension counts.
- Cryptographic hashing with SHA-256 and SHA3-256 for citation/provenance.
- Header/signature inspection using the first bytes of selected files.
- Limited text/XML parsing for `figuredata.xml`, `animation.xml`, `draworder.xml`, `partsets.xml`, `external_variables.txt`, `external_texts.txt`, `furnidata`, and `productdata`.
- Limited string-count inspection of binary `.cct` containers to classify likely component boundaries.

No assets were extracted, transformed, republished, or embedded into the project. No proprietary packet maps were reconstructed. No instructions are included for bypassing security, decompiling protected clients, or cloning a live service.

Evidence labels used in this report:

- **Direct evidence**: observed directly from file names, headers, hashes, text/XML content, counts, or metadata.
- **Strong inference**: highly likely based on multiple signals, but not proven by executing the legacy client.
- **Weak inference**: plausible, but needs deeper authorized tooling or runtime validation.
- **Open unknown**: not determined from this pass.

## 3. File Inventory and Classification

| Category | Evidence | Count / Size | Confidence | Notes |
|---|---:|---:|---|---|
| Director/Shockwave cast containers | `.cct`, `.dcr`, `XFIR`, `RIFX` signatures | 2,132 files total | High | Primary runtime/content packaging format. |
| Main bootstrap container | `habbo.dcr` | 13,468 bytes | High | Small Director bootstrap or loader-style entry. |
| Furniture/content libraries | `furni/` directory | 1,929 files | High | Dominates the package; mostly `hh_furni_xx_*` and sound samples. |
| Badge images | `c_images/Badges` | 237 files in `c_images`, mostly badges | High | Mostly GIF 31-50 px range, plus PNG/BMP/Thumbs.db. |
| Avatar metadata | `figuredata.xml`, `figuredata.txt`, `animation.xml`, `draworder.xml`, `partsets.xml` | 5 key files | High | Defines avatar parts, colors, animations, render order, active parts. |
| Furniture/product metadata | `furnidata`, `productdata` | 163,856 and 121,129 bytes | High | Furniture class metadata and product presentation strings. |
| Runtime configuration | `external_variables.txt` | 133 keys | High | Cast URLs, dynamic loading, navigator visibility, interface commands, security cast path. |
| Localization/UI text | `external_texts.txt`, `external_textsnorwegian.txt` | 2,773 and 2,597 keys | High | UI strings, registration, moderation, catalog, pets, games, badges, alerts. |
| Placeholder/missing files | `.cct` files with HTML 404 content | 30 signatures beginning `<!DO` | High | Not all `.cct` paths are valid Director containers. |
| Security/loader support | `sec.cct`, `security/sec.cct` | identical SHA-256 | High | Duplicate security cast file. Treat as legacy loader artifact, not reusable security model. |

Extension summary:

| Extension | Count |
|---|---:|
| `.cct` | 2,131 |
| `.gif` | 234 |
| `.txt` | 5 |
| `.xml` | 4 |
| no extension | 2 |
| `.dcr` | 1 |
| `.nfo` | 1 |
| `.db` | 1 |
| `.php` | 1 |
| `.png` | 1 |
| `.jpg` | 1 |

Signature summary:

| Signature | Count | Meaning |
|---|---:|---|
| `58 46 49 52` | 2,093 | `XFIR`, Director/Shockwave-style little-endian container. |
| `52 49 46 58` | 9 | `RIFX`, Director/Shockwave-style big-endian container. |
| `47 49 46 38` | 231 | GIF image files. |
| `3c 21 44 4f` | 30 | HTML `<!DOCTYPE...`, mostly 404 placeholders. |
| `3c 3f 78 6d` | 2 | XML declaration. |
| `ef bb bf 5b` | 2 | UTF-8 BOM + JSON-like/list data. |

## 4. Provenance and Authentication Notes

Direct evidence:

- `about dcr.txt` and `about dcr.nfo` describe the package as a V28 DCR pack with English and Norwegian texts, `c_images`, `furni`, `furnidata`, `productdata`, XML files, and a security cast.
- Several missing `.cct` placeholder documents reference `r28_20081202_1744_10717_6c80a6dd09d60c84a1396e0ceb63e445`.
- `external_variables.txt` references local URLs such as `http://localhost/r28/...`, suggesting this pack was prepared for a local/private loader environment, not preserved as an untouched production CDN mirror.

Strong inference:

- The package is a community-assembled V28 preservation/retro pack, likely based on late-2008 client resources.
- It is not a complete authoritative server implementation. It is a client/content bundle plus metadata and loader configuration.
- Some expected game libraries are missing and represented by 404 HTML placeholders. That matters for preservation because a runtime relying on every `.cct` path would fail unless those missing files are stubbed or replaced with original clean equivalents.

Important caution:

- The `about dcr` text itself says the pack came from a community forum and warns about modified downloads. Treat the source as untrusted until all files are hashed, scanned, and stored read-only.
- Do not execute legacy binaries or plugin-era content as trusted software.

Selected SHA3-256 citations:

| File | SHA3-256 |
|---|---|
| `habbo.dcr` | `77f8304212b5310a0728b73a4593ff1ab6cb8e996182db55581c2500d140da27` |
| `figuredata.xml` | `cdb3b29fa762b5349515cab2ddb9db5aca200e8e39c50646735a8ed22ae5f037` |
| `furnidata` | `5568c9e91efa4cf276701896450e892d28ee75f964a3a5fd02fe5c135e9acef6` |
| `animation.xml` | `78434b27a2b11e06e3a0d1e4a3c68b3b0e4136153cc80388540e65d0300385c2` |
| `draworder.xml` | `afe84b6321ce333cefda2403e0e51b3eddac0647a3ba90b10b70801496a81f92` |
| `external_variables.txt` | `fa61db7c3d0add9eb9bc1bd71da505c7808d4f3481407b8962b1f08f645fb6e0` |
| `external_texts.txt` | `d52e87dc79dbfecff4b7b1c86c32a99150057c079fe90d5121d26ed6cef01ea8` |
| `productdata` | `4e34fe66d46e2cf8decfdadfbd0743e3ca4081299cf239c4efb53deca7d025ba` |
| `hh_interface.cct` | `264bb529d104b004c2f5463c0233db97b738797a94be5a8dbca44ca1a5801e0a` |
| `hh_navigator.cct` | `9ec37a49d4fc45524dc443686e769eb7bcdc8e4851b95c613bdf11f53e9f8166` |
| `hh_messenger.cct` | `5c31d5ffe81c98afbd774ff9ebc9beb2efbd93aa3d96f2c45b4cfd238991f043` |
| `hh_room.cct` | `12add85d997629a057dd90f2cc99cf5adbb5e3a06a53d5684a0094eed8d26807` |
| `hh_room_pool.cct` | `e988275be0971eb2965b67a4ca5b09df091a800addc757b05405aa37f9caa300` |
| `hh_human_hair.cct` | `de3f74048af9b20cf582b204bb755c5a763392a31be6cf2eedbd65ba300716b8` |
| `hh_cat_gfx_all.cct` | `c06666ff6ac38a253fc556973733c1cf36973d066a90fd621c7ce3c31a3a7622` |
| `fuse_client.cct` | `7e386abb0d3f269b708e9de5433180c44bdae80945884e0104e12722a2d9e881` |

## 5. Main Runtime Components Detected

| Component | Evidence | Role | Confidence |
|---|---|---|---|
| Bootstrap / loader | `habbo.dcr`, `hh_entry*.cct`, `hh_entry_init.cct`, `hh_dynamic_downloader.cct` | Client boot, localization/region entry, dynamic cast loading. | High |
| Core shared library | `hh_shared.cct`, `hh_interface.cct`, `hh_buffer.cct`, `fuse_client.cct` | Shared UI/runtime behavior and network-adjacent client logic. | Medium-High |
| Room engine | `hh_room.cct`, `hh_room_ui.cct`, `hh_room_utils.cct`, many `hh_room_*` public room libraries | Public/private room rendering, room UI, object/actor display. | High |
| Navigator | `hh_navigator.cct`, `external_texts` keys beginning `nav_` | Public/private room discovery, search, visible roots. | High |
| Messenger / social | `hh_messenger.cct`, `hh_instant_messenger.cct`, `hh_friend_list.cct`, `messenger` text keys | Friend list, instant messages, presence/social actions. | High |
| Avatar renderer | `hh_human*.cct`, `figuredata.xml`, `partsets.xml`, `draworder.xml`, `animation.xml` | Figure part composition, animation, draw order, accessories. | High |
| Catalog/economy | `hh_cat_*`, `hh_purse.cct`, `furnidata`, `productdata`, catalog text keys | Catalog UI, purse/wallet display, furniture definitions. | High |
| Furniture | `furni/`, `hh_furni_classes.cct`, `furnidata`, `productdata` | Furniture content definitions and cast libraries. | High |
| Badges/moderation/social status | `hh_badges.cct`, `c_images/Badges`, badge text keys | Badge rendering and identity/status signaling. | High |
| Pets | `hh_pets*.cct`, `pet` localization keys | Pet rendering/interaction systems. | Medium-High |
| Games | `hh_games.cct`, `hh_game_*` placeholders, game text keys | Games subsystem partially present; some specific libraries missing. | Medium |
| Trax/sound | `hh_soundmachine.cct`, `sound_machine_sample_*` files, productdata sound sets | Music/sound machine subsystem. | High |
| Security cast | `sec.cct`, `security/sec.cct`, `security.cast.load.url` | Legacy loader/security handshake support. | Medium; do not reuse as security model. |

## 6. UI and State Flow Map

Direct evidence from file boundaries and localization keys supports this high-level state map:

```text
Boot DCR
  -> external_variables loaded
  -> external_texts loaded
  -> required casts loaded through cast.entry / room.cast / dynamic.download.url
  -> entry/localization cast selected
  -> registration/name/check screens available
  -> authenticated hotel shell
      -> navigator public/private views
      -> room entry
          -> room cast / public-space cast resolution
          -> avatar cast + room cast + furniture cast composition
          -> live room UI
              -> chat
              -> user action menu
              -> item action menu
              -> trade/friend/ignore/moderation actions
      -> messenger / friend list
      -> purse / credits
      -> catalog
      -> badges / profile / club / tutorial / recycler / games
```

Important interpretation:

- The client architecture was asset-module driven. It did not appear to be a single monolithic bundle only.
- The entry/bootstrap layer likely loaded casts conditionally by region, feature, room type, or UI area.
- The text and variables layer controlled many visible behaviors without changing binary casts.
- Public rooms were separated into many dedicated cast libraries (`hh_room_pool`, `hh_room_theater_*`, `hh_room_park_*`, `hh_room_tv_studio_*`), which is useful evidence for a modern asset-bundle strategy.

## 7. Client Architecture Inference

### 7.1 Bootstrap layer

Direct evidence:

- `habbo.dcr` starts with `XFIR`.
- `hh_entry*.cct` files exist for multiple locales/regions.
- `external_variables.txt` has 47 `cast.*` keys and 19 `room.*` keys.
- `dynamic.download.url=http://localhost/r28/`.
- `security.cast.load.url=http://localhost/r28/security/sec.cct?t=%token%`.

Strong inference:

- The bootstrap loaded a minimal entry movie, then resolved external variables and external texts, then loaded required cast libraries dynamically.
- Region-specific entry files allowed language/site branding or deployment-specific UI without changing all core casts.

Modern translation:

- Replace this with a launcher/client boot manifest:
  - `clientVersion`
  - `assetManifestVersion`
  - `requiredBundles`
  - `locale`
  - `featureFlags`
  - `entryScene`
  - `contentHash`

### 7.2 UI layer

Direct evidence:

- `hh_interface.cct`, `hh_ig_interface.cct`, `hh_bbinterface.cct`, `hh_room_ui.cct`.
- `external_texts.txt` contains keys for registration, alerts, navigator, trading, badges, club, pets, games, errors, moderation, and UI commands.

Strong inference:

- UI was text-key driven and modular by feature.
- The interface likely had strongly separated panels/windows: navigator, messenger, purse, catalog, room controls, registration, tutorial, games.

Modern translation:

- Use a UI state machine independent from asset rendering.
- Keep localization as data, not hard-coded strings.
- Keep modal/window definitions as declarative UI metadata where possible.

### 7.3 Avatar renderer

Direct evidence:

- `figuredata.xml` has 11 set types, 654 total `<part>` nodes.
- Set types observed: `hr`, `hd`, `ch`, `lg`, `sh`, `ha`, `he`, `ea`, `fa`, `ca`, `wa`.
- `animation.xml` defines actions: `wlk`, `wav`, `spk`, `lsp`, `swm`, `sws`.
- `draworder.xml` defines per-action/per-direction part draw ordering.
- `partsets.xml` defines active/flipped/removal rules.

Strong inference:

- Avatar rendering was compositional: figure string -> part sets -> palette colors -> action/direction draw order -> frame sequence.
- Some actions map to walking, waving, speaking, listening/speaking pose, swimming, and swim-state transitions.

Modern translation:

- Implement an original avatar system with:
  - `AvatarDefinition`
  - `AvatarPart`
  - `AvatarPalette`
  - `AvatarAction`
  - `AvatarDrawOrder`
  - `AvatarAnimationClip`
- Use original art and original part IDs in production. The legacy taxonomy can guide structure, not content.

### 7.4 Room/public-space renderer

Direct evidence:

- 77 root `hh_room*` `.cct` files.
- Dedicated public room casts include pool, park, theater variants, TV studio variants, cafe, lobby, library, disco, hallway, orient, terrace, etc.
- `external_variables.txt` contains navigator public/private roots and room cast entries.

Strong inference:

- Public rooms were distributed as dedicated visual/behavior cast libraries rather than a single generic tiled-room asset bundle.
- Private rooms and public rooms likely shared core room UI/runtime but loaded different art sets and interaction affordances.

Modern translation:

- Do not embed room authority in assets.
- Use `RoomDefinition` + `RoomLayout` + `RoomAssetBundle`:
  - Server owns room existence, permissions, actors, items, placement.
  - Client owns rendering using versioned bundles.
  - Public room special behavior should be server-controlled through original scripts/events, not copied legacy scripts.

## 8. Asset Taxonomy

| Asset Group | Direct Evidence | Modern Equivalent |
|---|---|---|
| Core boot | `habbo.dcr`, `hh_entry*.cct`, `hh_dynamic_downloader.cct` | Desktop launcher + game client boot scene + signed manifest. |
| Cast libraries | `hh_*.cct`, `furni/*.cct` | Unity Addressables, Godot PCKs, or hashed WebGL asset bundles. |
| Avatar parts | `hh_human*.cct`, `figuredata.xml`, `draworder.xml`, `partsets.xml` | Original avatar part atlas + JSON/protobuf metadata. |
| Furniture | `furni/`, `furnidata`, `productdata` | Original item definitions + render bundles + server-owned inventory entities. |
| Badges | `c_images/Badges` | Original badge icon CDN with entitlement metadata. |
| Localization | `external_texts*.txt` | Versioned i18n JSON catalogs. |
| Runtime variables | `external_variables.txt` | Environment-specific client manifest and feature config. |
| Audio/music | `hh_soundmachine.cct`, `sound_machine_sample_*` | Original audio clips and sequencer metadata. |

## 9. Content Model and Data Concepts

| Concept | Evidence | Backend-owned? | Client-rendered? | Notes |
|---|---|---:|---:|---|
| Account/session | Registration and alert text, entry casts, security cast path | Yes | No | Client can display login state, not own authority. |
| Avatar profile | `figuredata`, human casts, registration text | Yes | Yes | Server owns selected figure; client renders. |
| Avatar animation | `animation.xml`, `draworder.xml` | Mixed | Yes | Server chooses allowed state; client animates presentation. |
| Public/private room | `hh_room*`, navigator keys | Yes | Yes | Server owns room membership and state. |
| Room item/furniture | `furnidata`, `furni/` | Yes | Yes | Server owns inventory/placement; client draws object. |
| Catalog/product | `productdata`, `hh_cat_*` | Yes | Yes | Server owns prices/offers; client displays. |
| Wallet/purse | `hh_purse.cct`, credit text keys | Yes | Yes | Economic state must never be client-authoritative. |
| Messenger/friends | `hh_messenger`, `hh_friend_list` | Yes | Yes | Server owns relationships and presence. |
| Badges/status | `hh_badges`, `c_images/Badges` | Yes | Yes | Server owns entitlements; client displays icons. |
| Moderation/actions | text keys for alerts, legal text, automute, moderator commands | Yes | Yes | Admin/mod actions require audit trails. |
| Pets | `hh_pets*`, pet text keys | Yes | Yes | Server owns pet state; client renders. |

## 10. Backend Expectations Implied by the Client

The artifact strongly implies a backend with at least these responsibilities:

1. Session/bootstrap service.
2. External variable and text delivery.
3. Dynamic cast/content delivery.
4. Navigator public/private room data.
5. Room entry and live room state.
6. Avatar figure/profile state.
7. Inventory and furniture ownership.
8. Catalog/product pricing and localization.
9. Wallet/purse balance.
10. Messenger/friend list/presence.
11. Trading and item transfer validation.
12. Badge entitlement display.
13. Moderation, muting, alerts, and safety flows.
14. Pet state and pet interactions.
15. Games subsystem entry points.
16. Sound machine / music content lookup.

Important modern rule: the client should never be authoritative for room entry, item ownership, catalog purchases, wallet balances, trading outcomes, sanctions, or staff actions.

## 11. Legacy Structure Strengths and Weaknesses

### Strengths

- Strong modularity by content domain: room, avatar, furniture, UI, navigator, messenger, catalog, badges.
- Text/config files allow behavior and copy changes without repackaging every binary cast.
- Avatar model is data-driven enough to preserve as a conceptual model.
- Public rooms as separate cast libraries prove a useful asset-bundle boundary.
- Localization and product data are externalized.

### Weaknesses

- Binary cast format is obsolete and unsafe to execute as trusted runtime.
- Some `.cct` entries are 404 placeholders, so package completeness is inconsistent.
- Configuration references `localhost` and retro-loader style paths; it is not deployable as-is.
- Legacy security cast should not be treated as real security.
- Asset and behavior may be tightly coupled inside cast libraries.
- Text/config values include brand-specific and era-specific assumptions.
- File naming is useful but not sufficient proof of runtime behavior.

## 12. Modern Original Reimplementation Blueprint

The right reimplementation is not “run these DCRs”. The right path is “preserve the architecture patterns and rebuild with original assets, contracts, and server authority”.

### Preserved concepts

- Modular boot manifest.
- Externalized localization.
- Avatar part taxonomy and draw-order concept.
- Room asset bundle separation.
- Navigator public/private discovery.
- Furniture metadata model.
- Catalog/product metadata model.
- Badge/status display.
- Messenger and social presence.
- Safety/moderation as first-class UI and backend flow.

### Complete redesigns

- Replace DCR/CCT execution with a modern client engine.
- Replace legacy cast loading with signed content manifests and hashed bundles.
- Replace legacy security cast with modern token-based session handoff.
- Replace any packet-derived assumptions with original, versioned message contracts.
- Replace legacy images/assets with original art unless there is explicit legal permission for preservation-only display.

### Recommended modern architecture

```text
CMS/Web Portal
  -> account login / registration
  -> launcher code generation
  -> account settings / safety pages

Desktop Launcher
  -> validates session code
  -> downloads/updates client
  -> selects environment/channel
  -> launches game client with short-lived token

Game Client
  -> loads boot manifest
  -> validates asset manifest
  -> connects realtime gateway
  -> renders avatar/room/UI from original bundles

Backend
  -> identity/session
  -> room runtime
  -> inventory/catalog/wallet/trade
  -> social/messenger/groups
  -> moderation/admin/audit
  -> content manifest service
```

### Component dependency map

```text
Identity
  -> Session
    -> Launcher handoff
      -> Client boot
        -> Realtime auth
          -> Navigator
          -> Room entry
            -> Room runtime
              -> Avatar presence
              -> Chat
              -> Furniture placement

Catalog
  -> Wallet ledger
    -> Purchase transaction
      -> Inventory grant
        -> Room placement
```

## 13. Implementation Roadmap

| Phase | Goal | Inputs From Artifact | Build Output | Exit Criteria |
|---|---|---|---|---|
| 0. Preservation inventory | Freeze evidence and avoid accidental mutation | Hashes, counts, file signatures | Read-only manifest and research docs | All files hashed; missing/404 entries identified. |
| 1. Domain model extraction | Convert concepts into original schemas | `figuredata`, `furnidata`, text keys | Original conceptual schemas, not copied IDs/assets | Schemas reviewed for server authority. |
| 2. Launcher/client boot | Build modern boot flow | `external_variables` concept | Signed manifest + launcher handoff | Client can boot with token and manifest. |
| 3. Avatar prototype | Rebuild avatar renderer with original art | Part taxonomy, draw order, animation concepts | Data-driven avatar renderer | Original avatar walks/speaks/waves from server state. |
| 4. Room prototype | Rebuild isometric room shell | Room cast separation concept | Original room renderer + server room state | User enters only after backend confirms presence. |
| 5. Furniture loop | Build inventory and placement | `furnidata` structure | Original item definitions and placement rules | Server prevents invalid placement and duplication. |
| 6. Catalog/economy | Build catalog, wallet ledger, purchases | `productdata`, purse/catalog concepts | Versioned catalog + transaction ledger | Purchases are idempotent and auditable. |
| 7. Social/moderation | Build messenger, reports, sanctions | messenger/moderation text evidence | Social graph + moderation console | Reports and sanctions are auditable. |
| 8. Content pipeline | Replace DCR with modern bundles | cast library modularity | Addressables/CDN manifest pipeline | Client rejects unsigned/mismatched bundles. |

## 14. What Not To Do

- Do not execute DCR/CCT artifacts as trusted runtime.
- Do not redistribute extracted sprites, badges, furniture, or room art.
- Do not copy proprietary packet formats or private message names.
- Do not let the client own economy, inventory, trade, or moderation outcomes.
- Do not assume every `.cct` is valid; at least 30 inspected `.cct` files are HTML 404 placeholders.
- Do not treat `sec.cct` as modern security.
- Do not use this artifact as a production content source unless legal permission is explicit and documented.

## 15. Open Questions

| Question | Status | Required Next Step |
|---|---|---|
| Are all `.cct` containers valid Director casts? | Partially known | Build a read-only validator for XFIR/RIFX structure and list invalid entries. |
| Which casts contain script behavior vs pure art? | Unknown | Authorized Director-aware metadata inspection; no asset extraction. |
| Which room libraries are complete enough for visual reconstruction? | Unknown | Validate referenced cast dependencies against `external_variables` and room cast references. |
| Are `productdata` and `furnidata` internally consistent? | Partially known | Parse line-based list format after escaping legacy quote placeholders. |
| Which localization keys are unused/dead? | Unknown | Cross-reference keys against strings in cast libraries. |
| Which public room casts map to navigator entries? | Strong inference only | Build a manifest crosswalk from `external_texts`, `external_variables`, and room file names. |

## 16. Final Engineering Conclusions

This V28 pack is valuable as digital archaeology because it preserves a modular virtual-world client structure: boot variables, localization, avatar composition, room/public-space bundles, furniture metadata, catalog/product data, badge identity, messenger/social UI, and safety/moderation copy. The artifact is not valuable as a production runtime dependency. It is too old, legally risky for redistribution, incomplete in places, and tied to obsolete Shockwave execution.

For Epsilon, the practical conclusion is clear:

1. Preserve the pack as read-only evidence.
2. Use hashes and inventory reports for citation.
3. Extract concepts, not assets.
4. Rebuild all runtime behavior in original code.
5. Keep the server authoritative for all state.
6. Replace DCR/CCT with signed modern content bundles.
7. Use launcher + boot manifest + client runtime as the modern equivalent of the old dynamic cast-loader architecture.

The most useful lesson for the current project is architectural, not visual: the old client was not “just a web page”. It was a launcher/boot/runtime/content system with clear modular boundaries. Epsilon should follow that discipline with a modern desktop launcher, original client, signed asset manifests, realtime backend, and strict separation between CMS, launcher, client, and emulator/runtime.
