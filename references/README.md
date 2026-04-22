# References

This folder is reserved for local, non-versioned input material used during development.

- `references/raw/` is **git-ignored**.
- runtime code under `src/` must never read from this folder
- deployable assets must not depend on files stored here
- normalized project data must enter the repository through controlled importers and canonical manifests

This keeps the repository clean while preserving a place for local working inputs outside versioned product code.
