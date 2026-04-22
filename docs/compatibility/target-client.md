# Target Client

## Platform Direction

Epsilon is not meant to stop at one revision.

The platform target is:

- multi-version Habbo compatibility
- stable core hotel services underneath
- explicit adapter layers around protocol, launcher/bootstrap, content visibility, and rendering/runtime expectations

That is how Epsilon can support older and newer hotel eras without turning the codebase into revision spaghetti.

## Initial Compatibility Baseline

Epsilon Emulator v1 should target a single Flash-era compatibility family:

- family: `RELEASE63`
- status: `selected for first implementation`
- reason: abundant public references, packet docs, and nearby production/client variants

## Why One Target First

Legacy emulator history shows that trying to support multiple eras too early leads to:

- mixed packet semantics
- hidden version-specific hacks
- brittle item behavior
- hard-to-debug room logic

The first implementation should be judged by fidelity, not by number of revisions claimed.

So the rule is:

- platform ambition: all major hotel eras
- implementation order: one stable family at a time

## Evidence Sources

This baseline is supported by public reference material including:

- Flash archive indexes with `RELEASE63` clients and packet documents
- legacy emulator projects used as behavior cross-checks
- content packs and production artifacts where provenance is acceptable

## Rules

- client behavior beats legacy emulator behavior
- documented packet fixtures beat assumptions
- uncertain behavior must be marked `inferred`
- compatibility notes live next to protocol specs

## Future Expansion

After `RELEASE63` reaches stability, later compatibility families should be added through adapter packages such as:

- `Epsilon.Protocol.Release63`
- `Epsilon.Protocol.Release64`
- `Epsilon.Protocol.Shockwave`
- future web or mobile-compatible families

Expansion should also include:

- content adaptation by client family
- launcher/bootstrap profiles by client family
- compatibility policies for room, catalog, avatar, and game surfaces
- original Epsilon-only extensions that are not forced onto older clients

The core domain should remain stable while protocol adapters evolve around it.
