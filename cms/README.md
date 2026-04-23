## CMS

This folder contains website and community-facing CMS packages for Epsilon.

Structure:
- `system/`
  - Node.js + TypeScript generation server
- `epsilon-access/`
  - public-facing CMS portal for homepage, login, register, and launcher access
- `goldfish-classic/`
  - `site/` static pages and assets
  - `config/` deployment and route configuration
  - `README.md` package-specific notes
- `habbohotel-2001/`
  - `site/` preserved 2001 website and modern wrappers
  - `config/` deployment and route configuration
  - `README.md` package-specific notes

Rules:
- Preserve era-specific visual identity where useful.
- Route hotel actions to Epsilon services instead of dead legacy links.
- Keep CMS packages separate from runtime services.
- Do not treat the CMS as the game client.
- Launcher and app handoff belongs to the CMS; hotel presence confirmation does not.
