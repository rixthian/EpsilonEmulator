# Epsilon.Protocol

This module is where compatibility gets explicit.

Responsibilities:

- packet id registries
- command registries
- serializers and parsers
- version-scoped protocol adapters
- fixture-backed compatibility tests
- manifest-driven packet metadata

The protocol layer translates between strange legacy packet shapes and a sane internal command model.

Protocol ids, packet metadata, and command bindings should be loaded from versioned manifests instead of being hardcoded into application logic.
