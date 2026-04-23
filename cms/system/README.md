# CMS System

This is the Node.js + TypeScript serving layer for preserved CMS generations.

Purpose:
- serve each preserved CMS generation from a clean registry
- keep historical packages separated by era
- avoid mixing legacy HTML dumps into runtime service code

Routes:
- `/` list available CMS generations
- `/api/sites` registry metadata
- `/sites/{siteKey}/` serve a preserved CMS package

Notes:
- each package must provide `config/site.json`
- each package must provide `site/index.html`
- this system is intentionally static-first
- dynamic launcher and diagnostics integration stays inside each package front-end
