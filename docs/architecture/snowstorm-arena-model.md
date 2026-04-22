# SnowStorm Arena Model

Date: 2026-04-21

This document defines the modern Epsilon shape for SnowStorm arena data.

## Goal

SnowStorm arenas must be content-driven. Arena geometry, props, spawn logic, and supply nodes should all live in normalized Epsilon manifests so the game runtime can load them consistently across compatibility profiles.

## Canonical Model

### Arena Definition

- `ArenaKey`
- `DisplayName`
- `VenueKey`
- `BoundsWidth`
- `BoundsHeight`
- `ThemeKey`
- `IsEnabled`

### Arena Object Placement

- `ObjectKey`
- `ObjectType`
- `X`
- `Y`
- `StateValue`
- `VariantValue`
- `Tags`

### Spawn Cluster

- `ClusterKey`
- `X`
- `Y`
- `Radius`
- `Capacity`
- `TeamKey` optional

### Supply Node

- `NodeKey`
- `NodeType`
- `X`
- `Y`
- `RespawnSeconds`

## Import Strategy

The importer should:

- read legacy arena `.dat` lines
- parse them into typed arena object placements
- read spawn cluster files into typed clusters
- read snow machine files into typed supply nodes
- emit a normalized Epsilon JSON manifest per arena

## Runtime Rule

The SnowStorm runtime should depend on Epsilon arena manifests only. Legacy files remain research inputs and migration sources, not runtime dependencies.
