# Game Runtime Contracts

Epsilon should treat games as first-class runtime modules, not as room-side hacks.

## Core Contracts

The game runtime needs explicit session state:

- `GameSessionState`
- `GamePlayerState`
- `GameTeamDefinition`
- `GameSessionStatus`

These contracts allow BattleBall, SnowStorm, Wobble Squabble, and future venues to share a common lifecycle:

- waiting
- preparing
- running
- finished
- cancelled

## Why This Exists

The useful lesson from mature hotel servers is that minigames become unmaintainable when they are encoded as loose room flags or item-side behavior.

Epsilon should keep:

- venue discovery in content and navigator
- round/session state in game runtime
- delivery projection in protocol adapters

## Rule

Rooms can host games, but room metadata is not the same thing as a game session.

The room tells us where the match is happening.

The game runtime tells us:

- who is in the match
- what phase the match is in
- team assignments
- live score state
- whether the match is public or private
