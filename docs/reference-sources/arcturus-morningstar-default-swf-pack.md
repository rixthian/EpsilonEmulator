# Arcturus Morningstar Default SWF Pack Reference

This source is treated as a structural reference pack for Epsilon import work. It is not a runtime dependency and raw assets must not be mounted directly into the launcher, client, CMS, emulator, or production content roots.

## Source

| Field | Value |
| --- | --- |
| Requested URL | `git@git.krews.org:morningstar/arcturus-morningstar-default-swf-pack.git` |
| Working clone URL | `https://git.krews.org/morningstar/arcturus-morningstar-default-swf-pack.git` |
| Commit | `a7cea19c8877fb31fa53108655e96d94e4ce7e8e` |
| Local reference path | `/Users/yasminluengo/Documents/Playground/reference-sources/arcturus-morningstar-default-swf-pack` |
| Reference policy | `reference_only_no_raw_asset_runtime_dependency` |

The SSH endpoint timed out locally, so the reference clone was made through HTTPS. The clone stays outside the Epsilon repository to avoid mixing large third-party asset payloads with Epsilon source code.

## Generated Epsilon Reference Files

| File | Purpose |
| --- | --- |
| `docs/reference-sources/arcturus-morningstar-default-swf-pack.ingest-profile.json` | Stable ingest profile that records source identity, commit, and local root. |
| `docs/reference-sources/arcturus-morningstar-default-swf-pack.collection.json` | Canonical source inventory grouped by artifact family. |
| `docs/reference-sources/arcturus-morningstar-default-swf-pack.client-root.json` | Neutral client-root manifest with game data, client packages, image library, furniture, avatar, catalog, and SQL sections. |
| `docs/reference-sources/arcturus-morningstar-default-swf-pack.technical-summary.json` | Condensed technical metadata for game data and selected SWF containers. Full symbol names are intentionally omitted. |

## Canonical Catalog Registration

The pack is now registered in the Epsilon catalog as a source and compatibility reference:

| Catalog File | Purpose |
| --- | --- |
| `catalog/ingest-profiles/arcturus-morningstar-default-swf-pack.manifest.json` | Canonical ingest profile pointing at the external local clone. |
| `catalog/client-builds/flash-classic/arcturus-morningstar-default-swf-pack-habbo.manifest.json` | Concrete SWF client build entry for `gordon/PRODUCTION/Habbo.swf`. |
| `catalog/client-roots/arcturus-morningstar-default-swf-pack.manifest.json` | Client asset root taxonomy for gamedata, gordon packages, furniture, public rooms, avatar assets, image library, catalog images, SQL references, and auxiliary game packages. |
| `catalog/launcher-profiles/arcturus-morningstar-default-swf-pack.manifest.json` | External-variable compatibility profile for import analysis only. URLs from this file must be allowlisted or rewritten before runtime use. |

Core SWF evidence:

| File | Size | SHA-256 |
| --- | ---: | --- |
| `gordon/PRODUCTION/Habbo.swf` | 7,620,902 bytes | `1de81b633ee01d7ba7fb55ca3b4fdc8e69403dd3c92cc99e6664ae7db0dfd3a8` |
| `gordon/PRODUCTION/HabboRoomContent.swf` | 189,101 bytes | `24a9fa5b03287c3462f756a2f19738deab714825eea8b68b232efc06ce60b09d` |

## Inventory Summary

| Artifact Family | Count | Use in Epsilon |
| --- | ---: | --- |
| `badges` | 24,550 | Badge taxonomy and badge pipeline scale reference. |
| `furnitures` | 19,542 | Furniture import volume, naming pressure, room item pipeline stress reference. |
| `catalogue` | 5,582 | Catalog visual taxonomy reference. |
| `icons` | 5,442 | Catalog/icon surface reference. |
| `image_library` | 3,802 | CMS/content image taxonomy reference. |
| `gordon` | 2,372 | Client package surface and avatar package taxonomy reference. |
| `badgeparts` | 242 | Badge composition pipeline reference. |
| `gamedata` | 27 | High-value metadata source for item, avatar, effect, localization, and launcher profile modeling. |
| `catalog_sql` | 15 | Catalog schema concept reference only; do not import directly into production tables. |
| `game_packages` | 2 | Auxiliary game package taxonomy reference. |
| `security_policy` | 1 | Legacy policy artifact; not a modern security model. |

## Technical Findings

Some legacy metadata files need tolerant readers. The technical summary uses structural counts for XML-like files because strict XML parsing can fail on malformed legacy tokens.

| Area | Observed Data | Epsilon Decision |
| --- | --- | --- |
| Furniture metadata | `gamedata/furnidata.xml` contains 10,793 `furnitype`-like entries by structural count. | Use as scale and field-shape reference for neutral item definitions. Do not copy legacy schema directly. |
| Avatar metadata | `figuredata.xml` contains 3 palettes, 543 colors, 13 set types, 2,350 sets, and 6,469 parts by structural count. | Preserve the idea of palette/set/part separation in the original Epsilon avatar model. |
| Figure map | `figuremap.xml` contains 2,069 library entries and 5,806 part mappings by structural count. | Use to validate that avatar rendering needs a library manifest layer separate from account/avatar state. |
| Effects | `effectmap.xml` contains 203 effect entries by structural count. | Model effects as server-entitled client presentation capabilities. |
| Localization/config | `external_flash_texts.txt` has 31,582 non-empty entries; `external_variables.txt` has 4,107. | Epsilon needs a typed localization/config pipeline, not hardcoded loader labels. |
| Catalog SQL | Includes catalog pages, catalog items, item base, featured pages, clothing, crafting, and emulator settings/texts. | Treat as domain discovery. Epsilon database remains original and migration-controlled. |
| SWF core package | `gordon/PRODUCTION/Habbo.swf` exposes component families around inventory, avatar rendering, windows, room events, catalog, sound, room UI, toolbar, friends, and navigator. | Use component families to guide modern subsystem boundaries; do not recreate proprietary internals. |
| Room content package | `HabboRoomContent.swf` exposes wall textures, landscapes, windows, floor textures, doors, room assets, and manifest/index symbols. | Epsilon room presentation must separate room state from room visual library manifests. |

## Mapping To Epsilon Systems

| Source Area | Epsilon Target |
| --- | --- |
| `gamedata/furnidata.xml` | `item-definition-import-model`, inventory definitions, catalog item compatibility checks. |
| `gamedata/figuredata.xml` and `gamedata/figuremap.xml` | Avatar part catalog, palette model, avatar rendering manifest. |
| `gamedata/external_flash_texts.txt` | Localization import surface. |
| `gamedata/external_variables.txt` | Launcher/client profile import surface after strict allowlisting. |
| `Catalog-SQLS/*.sql` | Catalog concept extraction and test fixture design, not direct production import. |
| `gordon/PRODUCTION/Habbo.swf` | Client module boundary evidence for modern client architecture. |
| `gordon/PRODUCTION/HabboRoomContent.swf` | Room content package boundary evidence. |
| `dcr/hof_furni/*.swf` | Furniture package volume and naming taxonomy reference. |
| `c_images/**` and `catalogue/**` | CMS and catalog content image taxonomy reference. |
| `game/*.swf` | Auxiliary game package classification reference. |

## Implementation Rules

1. Keep this pack outside the Epsilon repository and treat it as read-only reference input.
2. Generate manifests from the source root; do not copy bulk assets into `src/`, `wwwroot/`, launcher assets, or emulator runtime folders.
3. Any runtime content must pass through a neutral Epsilon normalization step with original IDs, ownership rules, hashes, and provenance.
4. Legacy `external_variables` values must be allowlisted before they influence launcher/client behavior.
5. Legacy SQL can inform concept discovery, but Epsilon schema changes must be authored as first-class migrations.
6. SWF analysis is limited to container metadata and high-level symbol family counts; no proprietary packet maps or private client internals are imported.

## Regeneration Commands

```bash
python3 tools/ingest/collect_assets.py \
  docs/reference-sources/arcturus-morningstar-default-swf-pack.ingest-profile.json \
  --output docs/reference-sources/arcturus-morningstar-default-swf-pack.collection.json
```

```bash
python3 tools/importers/client_asset_root_manifest_builder.py \
  /Users/yasminluengo/Documents/Playground/reference-sources/arcturus-morningstar-default-swf-pack \
  --root-id arcturus_morningstar_default_swf_pack \
  --output docs/reference-sources/arcturus-morningstar-default-swf-pack.client-root.json
```

## Next Import Priorities

1. Build a neutral `furnidata.xml` reader that extracts only canonical item definition fields required by Epsilon.
2. Build a neutral avatar manifest reader from `figuredata.xml` and `figuremap.xml`.
3. Add catalog concept tests using the SQL filenames and high-level table concepts, without importing SQL directly.
4. Add localization/config allowlists before consuming `external_flash_texts` or `external_variables`.
5. Add loader/client acceptance tests proving that no runtime path depends directly on this reference root.
