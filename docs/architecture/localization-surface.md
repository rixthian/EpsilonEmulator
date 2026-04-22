# Localization Surface

Interface language should not be hardcoded into templates, controller methods, or packet handlers.

## Epsilon Direction

Epsilon should load localized UI content through explicit bundles:

- portal text bundles
- client settings bundles
- navigator labels
- catalog landing copy
- staff/admin labels

The storage contract is represented by:

- `LocalizedTextBundle`
- `ILocalizedTextBundleRepository`

## Why This Matters

Language changes should be:

- dynamic per user
- shared across portal and in-client settings
- versionable
- testable

## Rule

Feature code chooses keys.

Localization bundles provide text.

No feature surface should depend on embedded English literals for user-facing UI when a localized bundle exists.
