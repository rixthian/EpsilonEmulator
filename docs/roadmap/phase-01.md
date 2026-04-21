# Phase 01 Roadmap

## Goal

Reach a trustworthy vertical slice for one Flash compatibility target.

## Milestones

1. Source Catalog Foundation
   Build the curated reference catalog and classify all major source packages.

2. Protocol Baseline
   Define handshake, auth, ping, user object, room entry, movement, and chat packets.

3. Auth Slice
   Implement account, session, SSO/ticket validation, and ban checks.

4. Room Slice
   Allow users to enter a room, spawn, move, and chat.

5. Inventory Slice
   Load inventory, place basic items, and persist room/item state.

6. Admin Slice
   Expose health, active sessions, and moderation inspection endpoints.

7. Regression Foundation
   Add packet fixtures and room simulation tests.

## Exit Criteria

- one selected client can connect successfully
- character can authenticate and enter a room
- movement and chat are stable
- protocol fixtures protect against regressions
- behavior notes are source-backed for implemented features

