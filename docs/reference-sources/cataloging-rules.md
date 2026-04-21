# Reference Cataloging Rules

Epsilon Emulator depends on historical references, but those references vary widely in quality and trustworthiness. The catalog must distinguish signal from noise.

## Source Tiers

### Tier 1

High-confidence references:

- original client builds
- packet lists
- official or near-official content manifests
- production texts/vars/furni data with clear provenance

### Tier 2

Medium-confidence references:

- emulator source code
- emulator SQL dumps
- CMS and launcher projects
- community docs with reproducible evidence

### Tier 3

Low-confidence references:

- forum snippets
- unnamed repacks
- partial source leaks
- undocumented edited SWFs

### Rejected

Never ingest as trusted references:

- hacks and cheat tools
- cracked binaries
- malware-risk executables
- repacks with no provenance and no unique value

## Required Catalog Fields

Every catalog entry must record:

- stable id
- title
- source url
- archive/package name
- category
- estimated era
- trust tier
- legal/provenance notes
- signals extracted
- implementation relevance

## Evidence Labels

When documenting behavior, each claim should use one of:

- `confirmed`
- `cross-checked`
- `inferred`
- `unknown`

## Decision Priority

1. client/runtime evidence
2. packet docs
3. multiple independent emulator confirmations
4. single emulator behavior
5. community folklore

