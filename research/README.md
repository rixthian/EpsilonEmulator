# Research Boundary

This folder exists to define a strict boundary between:

- product code
- architecture and runtime configuration
- external research material

## Rules

- runtime code under `src/` must not depend on archive links
- deployment and configuration files must not embed historical source links
- public reference URLs should stay out of operational code paths
- research evidence should be maintained separately from shipping runtime assets

## Current Policy

This repository keeps the emulator runtime clean.

Historical archives and legacy emulator sources are valid research inputs, but their direct URLs and raw collections should not be mixed into the runtime surface of the project.

## If Research Needs To Be Tracked

Track it through:

- internal identifiers
- offline notes
- private provenance records
- importer-ready normalized datasets
- architecture translations that describe what Epsilon should keep and what it should discard

Do not let the runtime or deployable artifacts depend on public archive links.
