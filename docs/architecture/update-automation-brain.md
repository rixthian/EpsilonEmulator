# Update Automation Brain

Epsilon needs an update-intelligence subsystem that is separate from gameplay runtime.

That subsystem is the asset-intelligence brain.

## Purpose

The brain exists to:

- observe public upstream changes
- detect deltas in gamedata and client artifacts
- classify those deltas into Epsilon import actions
- keep update automation reproducible

It does not exist to turn the emulator into a web scraper glued directly to runtime.

## Placement

The brain lives outside the hotel runtime:

- `tools/brain/`

This is intentional.

Gameplay services should never depend directly on:

- remote hotel URLs
- live scraping
- decompilers
- content diff heuristics

## Internal Shape

The brain should evolve as five steps:

1. `SourceWatch`
   watches public source locators and captures metadata or body hashes
2. `SnapshotStore`
   stores normalized source snapshots
3. `DiffEngine`
   compares old and new snapshots
4. `RecommendationEngine`
   maps changes to Epsilon importer actions
5. `Publisher`
   publishes reviewed update candidates into canonical manifests

## Trusted SWF Toolchain

Epsilon should not trust random SWF repositories or ad hoc patchers.

The intended toolchain is:

- `JPEXS FFDec`
- `RABCDAsm`
- `Apache Flex SDK`

Those tools belong to an offline content pipeline, not to runtime.

## Security Boundary

The brain may work with:

- public URLs
- public hashes
- public metadata
- local source archives supplied by the operator

The brain must not implement:

- key extraction
- DRM bypass
- encrypted asset circumvention
- Widevine-like evasion logic

If an artifact is protected, the correct output is:

- `manual review required`

not:

- automatic decryption

## Relationship To The Server

The current server already has:

- diagnostics
- protocol monitoring
- intelligence summaries

The brain should feed those systems through reviewed canonical outputs, not through direct runtime mutation.

Correct order:

1. brain detects a change
2. operator reviews the diff
3. importer regenerates canonical manifests
4. launcher/gateway/runtime consume canonical outputs

## Immediate Deliverables

The first serious deliverables are:

- watch profile configuration
- source snapshot capture
- diff report generation
- toolchain policy definition

That foundation is enough to support scheduled automation later.
