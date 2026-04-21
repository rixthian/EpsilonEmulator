# Hotel Domain Blueprint

This blueprint translates the strongest recurring concepts found across mature legacy hotel products into clean Epsilon domain boundaries.

## Core Bounded Contexts

### Identity

- account
- character
- session
- subscriptions

### Rooms

- room definitions
- room layout definitions
- room settings
- room item state
- room rights
- room visits

### Content

- item definitions
- catalog pages
- catalog offers
- navigator public-room entries
- public-room asset package definitions
- text resources
- pet definitions

### Social

- messenger
- tags
- groups

### Moderation

- bans
- moderation tickets
- staff actions
- room visit and chat evidence

### Automation And Interaction

- item interaction taxonomy
- wired triggers
- wired actions
- future wired conditions

## Modeling Rules

- room layout is distinct from room instance metadata
- public-room asset package identity is distinct from room layout identity
- catalog pages are distinct from catalog offers
- item definition is distinct from item instance
- pet definition is distinct from pet profile
- interaction type is content metadata, not ad hoc switch sprawl in random services

## Why This Shape

This shape is the clean modern translation of what old mature emulators eventually converged toward:

- richer room model support
- unified item/content concepts
- explicit subscriptions and pets
- formal interaction systems
- navigator and catalog as first-class content domains
