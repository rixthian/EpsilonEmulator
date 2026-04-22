# Item Definition Import Model

Epsilon treats legacy `furnidata` as an import source, not as runtime truth.

The import pipeline produces two outputs:

- a rich `item definition manifest` for archival, inspection, and future transforms
- a projected `item-definitions.json` seed file that matches Epsilon's runtime `ItemDefinition` contract

This separation is deliberate.

The legacy sources are inconsistent by era:

- text variants expose less structural metadata
- XML variants expose dimensions and placement flags more clearly
- neither variant should dictate the final emulator domain directly

The canonical rich manifest keeps source-facing details such as:

- revision
- width and length
- default direction
- colors
- offer binding
- ad URL
- source class name
- source variant

The runtime seed projection keeps only what `Epsilon.Content.ItemDefinition` needs today:

- item identity
- public and internal names
- type code
- sprite binding
- placement and interaction booleans
- interaction type and mode count

Current importer behavior:

- supports `furnidata.txt`
- supports `furnidata.xml`
- uses conservative heuristics to infer interaction type when legacy input does not state it directly
- keeps sprite binding equal to the legacy item definition id when no cleaner sprite identity is present

That heuristic layer is acceptable at import time.
It should not leak into runtime services.

Repository rule:

- raw generated manifests from third-party datasets do not need to be committed
- the repo keeps the importer, schema, and curated seed path
- large dataset exports can be generated locally when needed
