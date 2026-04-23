# Goldfish Classic CMS

This package is a reconstructed early-era Goldfish/Habbo-style CMS adapted for Epsilon.

What it is:
- a preserved 1999-2000 style community site
- updated to point at Epsilon launcher, gateway, and admin surfaces
- structured as a standalone CMS package instead of a loose archive dump

Folders:
- `site/` static HTML pages and front-end assets
- `config/` deployment-level configuration reference

Operational intent:
- front page for hotel entry and news
- client entry page
- help, safety, and terms pages
- lightweight live status checks against Epsilon endpoints

Current assumptions:
- the site is hosted alongside, or proxied toward, Epsilon services
- same-origin status calls are preferred
- launcher/admin URLs can be adjusted in `config/site.json`
