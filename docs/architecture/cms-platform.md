# CMS Platform

Epsilon needs a modern CMS platform, not a retro control panel glued to tables.

## Purpose

The CMS is the product control plane for:

- web surfaces
- mobile app surfaces
- operations and moderation
- live hotel content
- event scheduling
- player identity lookup

## Core Rule

Internal identity and public identity must be separated.

- `CharacterId`
  internal numeric runtime key
- `PublicId`
  stable public player identifier for CMS, app links, profiles, and external-safe lookups

The public web, mobile, and CMS surfaces should prefer `PublicId` instead of exposing sequential internal ids.

## CMS Scope

The CMS should eventually own:

- player directory and profile tooling
- groups and forums management
- catalog, campaigns, and events
- moderation workflows
- hotel news and editorial surfaces
- badges and collectible visibility
- mobile-safe APIs

## Client Surfaces

The correct split is:

- hotel runtime
  realtime gameplay
- launcher/bootstrap
  connection and client capability negotiation
- CMS/API
  content, operations, editorial, and account-facing product surfaces
- mobile app
  consumes CMS/API and selected hotel-safe endpoints

## Mobile Direction

A smartphone app should not depend on Flash-era assumptions.

It should consume:

- public player identity
- profile snapshots
- inventory/account summaries
- groups, messages, and events
- notifications and lightweight room presence summaries

## Immediate Foundation

The first concrete step is already in place:

- stable `PublicId` on character profiles
- public-id lookup endpoints in gateway and admin/CMS surfaces

That lets the project move toward:

- shareable profile URLs
- app-safe player lookup
- CMS-side player search and moderation tools
- cross-device identity without exposing internal sequence ids

## Current implementation status

Epsilon now has a real CMS platform slice:

- homepage
- login
- register
- authenticated launcher access choice
- launcher access-code generation
- home payloads for news, photos, leaderboard, support, and hotel status

The CMS rule stays strict:

- the CMS authenticates and informs
- the launcher/app starts the client
- the emulator confirms real hotel presence

## Current risk

The CMS surface is still unstable.

That instability is mostly in:

- visual/product polish after rapid iteration
- launcher handoff hardening
- persistence continuity because too much state still depends on `InMemory`

So the CMS is now a real platform surface, but not a finished one.
