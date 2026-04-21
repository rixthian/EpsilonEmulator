# Protocol Baseline

This document defines the first packet set Epsilon should implement for the initial vertical slice.

## Incoming Candidate Packets

- `init_crypto`
- `ssoticket`
- `get_info`
- `enter_room`
- `move_avatar`
- `chat`

## Outgoing Candidate Packets

- authentication ok
- user object
- credits/activity balances
- room open
- room user object list
- room status updates
- chat event

## Confidence Notes

This list is a starter baseline only. Exact ids and structures must be validated against packet documents and target-client fixtures before they are treated as confirmed.

## Implementation Rule

Every packet implemented in code should have:

- a spec entry
- a provenance note
- at least one fixture or snapshot test

