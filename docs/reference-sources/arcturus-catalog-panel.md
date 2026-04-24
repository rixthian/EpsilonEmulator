# Arcturus Catalog Panel Reference

This source is a catalog administration reference only. It is useful for understanding a practical visual catalog editing workflow, but it must not be copied into Epsilon as a production admin surface.

## Source Snapshot

| Field | Value |
| --- | --- |
| Requested source | `git@github.com:xKiwiRetro/Arcturus-Catalog-Panel.git` |
| Working clone URL | `https://github.com/xKiwiRetro/Arcturus-Catalog-Panel.git` |
| Local reference path | `/Users/yasminluengo/Documents/Playground/reference-sources/Arcturus-Catalog-Panel` |
| Observed commit | `3d4e48a3b3b2ba9a5fb812845ffef4fd45f68a98` |
| Primary language | PHP |
| Database target | MySQL / MariaDB with Arcturus or Morningstar-style catalog tables |
| Runtime | PHP 7.4+, webserver |
| License file | No license file observed in the checked source root |
| Epsilon policy | Architecture and UX reference only; no direct runtime dependency |

SSH access failed locally because GitHub host key verification was not configured in this environment, so the repository was cloned through HTTPS for analysis.

## Observed Files

| File | Role |
| --- | --- |
| `README.md` | Project overview, setup instructions, features, and dependency notes. |
| `index.php` | Main visual interface and frontend logic. |
| `api.php` | AJAX backend handler for catalog page loading, editing, sorting, moving, creating, and deleting. |
| `db.php` | PDO database connection bootstrap from local config. |
| `config.sample.php` | MySQL credentials, language selection, and image/icon URL configuration. |
| `languages.php` | UI language strings. |
| `test.php` | Local test surface. |

## Useful Product Ideas

| Idea | Why It Matters | Epsilon Translation |
| --- | --- | --- |
| Visual catalog tree | Staff should not edit catalog hierarchy directly in database tables. | Build an admin catalog tree view backed by `Epsilon.AdminApi`. |
| Drag and drop ordering | Catalog order is operational content, not developer-only configuration. | Implement audited reorder commands with optimistic concurrency. |
| Cross-tab moving | Staff need quick page reclassification during campaign setup. | Implement `MoveCatalogPage` commands with validation and rollback. |
| Live image previews | Catalog editors need immediate feedback for headline, teaser, and icon assets. | Use CDN-backed preview resolution through an allowlisted asset service. |
| Live search | Large catalogs need fast operational navigation. | Use a catalog read model and indexed search endpoint. |
| Multi-language labels | Staff tooling needs localized labels and multilingual catalog copy. | Keep admin UI localization separate from runtime/game localization. |
| Hybrid layout editor | Some catalog pages require known layouts, while special campaigns need custom layouts. | Maintain a server-side catalog layout registry with explicit compatibility flags. |

## Observed API Actions

| Action | Observed Behavior | Epsilon Decision |
| --- | --- | --- |
| `get_tree` | Reads `catalog_pages` and returns nested HTML. | Return structured JSON, not server-generated admin HTML. |
| `get_page` | Reads one catalog page and item count. | Return typed DTOs from Admin API. |
| `update` | Updates catalog page fields directly. | Use command handlers with validation, audit logging, and version checks. |
| `sort` | Updates nested `order_num` and `parent_id` from a submitted tree. | Use transactional reorder commands and reject stale revisions. |
| `sort_tabs` | Reorders root tab ids. | Use a dedicated tab ordering command. |
| `move_to_tab` | Updates `parent_id` for a page. | Validate destination permissions, page compatibility, and hierarchy rules. |
| `create_child` / `create_root` | Inserts enabled, visible pages with default layout. | Create draft pages first, then publish intentionally. |
| `delete` | Deletes a catalog page by id. | Prefer archive or soft delete with restore support. |

## Risks If Copied Directly

| Risk | Why It Is Dangerous |
| --- | --- |
| Direct database writes from the admin UI | Bypasses domain validation, audit policy, publication workflow, and compatibility rules. |
| No observed authentication boundary in `api.php` | A catalog tool must be staff-only and role-scoped. |
| No observed CSRF protection | Browser-based staff tools need CSRF or same-site token protection. |
| No observed audit log | Catalog changes affect economy, purchases, and player trust. Every change must be attributable. |
| Hard delete behavior | Staff mistakes can remove active catalog content without recovery. |
| HTML response generation inside the API | Couples backend transport to one UI implementation and makes contract testing harder. |
| No draft/publish model | Live catalogs should not mutate immediately while staff are still editing. |
| No catalog versioning | Launcher, client, and server need compatible catalog snapshots. |

## Epsilon Integration Decision

Epsilon should preserve the workflow concept, not the implementation style.

The correct target is an internal Catalog Admin Tool:

```text
Admin browser UI
  -> Epsilon.AdminApi
  -> Catalog application commands
  -> Catalog service / content store
  -> Audit log + versioned catalog snapshot
  -> Runtime/catalog read models
```

The CMS must not own catalog mutation. The public CMS may display catalog-related community content, promotions, and download/launcher entry points, but staff catalog editing belongs in the admin surface.

## Implementation Notes For Epsilon

- Add `CatalogPageDraft`, `CatalogPageVersion`, and `CatalogPublication` concepts before building advanced staff UX.
- Build catalog admin APIs as JSON contracts with explicit DTO schemas.
- Require staff RBAC for every mutation.
- Record before/after values in an append-only audit log.
- Validate image/icon references through an asset manifest service instead of accepting arbitrary URLs.
- Use soft delete for pages, offers, and campaigns.
- Make publication atomic so a bad partial save cannot corrupt the live catalog.
- Keep compatibility projection separate from canonical catalog editing.

## Related Epsilon Documents

- [catalog-admin-tooling.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/catalog-admin-tooling.md)
- [catalog-adaptation-strategy.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/catalog-adaptation-strategy.md)
- [web-surface-split.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/web-surface-split.md)
- [arcturus-community.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-community.md)
