# Web Surface Split

Date: 2026-04-21

This document fixes the boundary between hotel runtime, public website, and staff surfaces. The reference audits consistently show that trying to collapse all three into one application produces coupling and operational drift.

## Required Split

Epsilon should keep three separate web-facing surfaces:

- `Epsilon.Gateway`
  Handles hotel runtime APIs, session-bound gameplay actions, runtime snapshots, and protocol-adjacent HTTP services.
- future public portal/community application
  Handles login, registration, community, groups, homes, referrals, events, and user settings.
- `Epsilon.AdminApi`
  Handles housekeeping, moderation, catalog operations, support operations, and staff-only feature controls.

## Why This Split Matters

- gameplay latency and operational requirements differ from community and admin traffic
- staff tooling needs stronger authorization and audit requirements
- public website flows evolve faster than protocol/runtime compatibility
- social/community features should not share the same deployment and security surface as mutable room actions

## Current Translation Status

Already translated into Epsilon:

- runtime language preference APIs live in the gateway
- housekeeping/staff surfaces already have a separate admin API
- externalized catalog and world feature state can be consumed by both runtime and future website surfaces

Still missing:

- public portal/community web application
- website-facing group and event APIs
- proper website auth and registration flows
- moderation workflows that bridge gateway actions and admin review tools

## Design Rule

When adding a feature, place it by responsibility first:

- if it mutates or reads active hotel runtime state, it belongs in the gateway or protocol surface
- if it is user-facing community or account experience, it belongs in the public portal/community surface
- if it is staff-controlled, privileged, or operational, it belongs in the admin surface

This keeps Epsilon modern, maintainable, and compatible across multiple client/runtime families.
