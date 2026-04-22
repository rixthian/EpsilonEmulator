# Modern Auth Cryptography

Epsilon must not use legacy emulator hashing patterns such as:

- MD5
- SHA1 password hashes
- UTF7-based hashing
- custom unsalted digest formats

## Current Epsilon Baseline

Epsilon now uses a versioned password-hash record format with:

- `PBKDF2-HMAC-SHA256`
- `600000` iterations minimum
- `32`-byte salt
- `32`-byte subkey
- constant-time verification

This is the current built-in baseline because it is available in modern .NET without introducing runtime fragility.

## Why This Choice

- it is supported directly by the .NET runtime
- it avoids weak legacy patterns still common in old emulators
- it gives Epsilon an explicit format/version boundary for future upgrades

## Future Upgrade Path

If Epsilon later adopts an external memory-hard password hasher, the preferred target is:

- `Argon2id`

That upgrade should be implemented by adding a new `PasswordHashAlgorithm` value and rehashing on successful login.

## Scope

This password cryptography is separate from:

- session ticket generation
- packet framing
- transport protocol negotiation
- room/game runtime signaling

Modern emulators typically operate with:

- HTTPS or API launcher/bootstrap
- secure random SSO/session tickets
- packet or websocket transport depending on client family
- Redis for hot shared state
- PostgreSQL for durable state
- CDN/object storage for assets

Password hashing is one security layer inside that larger system, not the transport layer itself.
