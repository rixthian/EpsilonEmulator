# References

This folder is where raw legacy reference material lives on a developer's machine.

- `references/raw/` is **git-ignored** (see `.gitignore`). Drop SWF clients, old emulator sources, furnidata dumps, external texts, SQL backups, etc. inside there.
- Runtime code under `src/` must never read from this folder and must never embed URLs that point at archives. Research enters the project through the importers in [`tools/importers/`](../tools/importers/README.md), which emit canonical Epsilon data formats into `catalog/` and `sql/`.
- Analyses written about reference material live under [`research/`](../research/README.md), not here.

If you are cataloging a new source, start with [`docs/reference-sources/cataloging-rules.md`](../docs/reference-sources/cataloging-rules.md).
