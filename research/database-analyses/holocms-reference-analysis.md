# HoloCMS Reference Analysis

Source analyzed:

- [HoloCMS](/Users/yasminluengo/Downloads/HoloCMS)

## Summary

HoloCMS is useful as a reference for product surface and legacy client-launch integration.

It is not useful as a software foundation.

The codebase is a 2008 PHP monolith with:

- direct page scripts
- deprecated `mysql_*` database access
- weak session handling
- inline HTML generation
- hardcoded configuration and secrets
- no separation between CMS, launcher, moderation, and hotel runtime concerns

## What Is Useful For Epsilon

### 1. Product surface discovery

The file layout confirms which website-facing features existed around the hotel:

- account management
- registration and forgot password
- client launcher
- credits and club purchase flows
- community pages
- groups
- forums
- minimail
- user profiles and home/customization features
- housekeeping / moderation / admin tools

This is useful for deciding what belongs in:

- core runtime
- admin API
- web frontend
- optional future modules

### 2. Client launcher contract

`client.php` is useful because it shows the practical launcher concerns for Director-era clients:

- DCR source
- host and port injection
- SSO ticket injection
- external texts path
- external variables path
- room forwarding
- client reload / fatal error callbacks

These are real integration concepts Epsilon should preserve, but with a modern launcher service and typed configuration.

### 3. Configuration categories

`core.php` and `config.php` show the types of settings legacy CMSs had to manage:

- site path
- hotel short name
- game host and port
- DCR path
- external texts and variables
- maintenance mode
- language
- SSO enablement

Epsilon should keep these concerns, but move them into:

- typed options
- environment-specific configuration
- validated manifests

### 4. Admin/housekeeping scope

The `housekeeping/` directory is useful as a feature inventory for staff tooling:

- alerting
- bans
- chatlogs
- credits
- vouchers
- news
- user management
- wordfilter
- config editing

This helps define the future `AdminApi` and admin frontend surface.

### 5. Asset and community structure

The presence of:

- `web-gallery`
- `habbo-imaging`
- `locale`
- homes widgets and stickers

is useful for identifying content-serving and community-facing subsystems that may later need adapters or standalone services.

## What Should Not Be Reused

### 1. Runtime architecture

The code is strongly page-driven and tightly coupled:

- PHP scripts directly query and mutate the database
- configuration is loaded globally
- session, auth, and rendering are mixed
- HTML, control flow, and persistence all live together

Epsilon should not inherit any of that.

### 2. Authentication model

`core.php` shows weak auth/session practices:

- session handling through legacy PHP globals
- remember-me implemented via fragile cookies
- SSO ticket generation inside the CMS layer
- password and session concerns mixed into page scripts

Epsilon should replace this with:

- dedicated auth service
- explicit session store
- secure cookie/session policy
- separate launcher token issuance

### 3. Database access

The codebase uses deprecated `mysql_*` calls and string-built queries throughout.

This is not reusable. It should be replaced by:

- typed repositories
- parameterized queries
- explicit transaction boundaries
- domain-focused persistence models

### 4. Security posture

The sample configuration includes hardcoded credentials and the code contains direct query concatenation.

This means the codebase is useful for historical understanding, not for implementation reuse.

## Concrete Epsilon Value

HoloCMS is useful in four specific ways:

1. define the launcher configuration contract
2. define the future admin surface
3. define which community features should be optional modules
4. confirm that website and hotel runtime must be separated

## Modern Translation For Epsilon

Instead of reusing HoloCMS patterns, Epsilon should translate the same needs into modern components:

### Replace old PHP page scripts with:

- `ASP.NET Core` web applications or APIs
- typed service layers
- component-based frontend if needed

### Replace inline launcher logic with:

- a dedicated launcher endpoint
- versioned client manifests
- environment-aware runtime configuration

### Replace housekeeping pages with:

- `AdminApi`
- audited staff actions
- role-based authorization
- structured moderation logs

### Replace global config rows with:

- strongly typed options
- environment variables
- validated JSON/TOML manifests

## Bottom Line

HoloCMS is worth studying for:

- product features
- launcher inputs
- admin scope
- historical integration patterns

It is not worth inheriting for:

- architecture
- authentication
- persistence
- security model
- deployment model

