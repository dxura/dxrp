---
title: Content Entries
description: Expose addon entities, equipment, systems, and fixtures to game modes with stable references and configuration.
order: 2
---

A content entry is the portal definition that exposes one addon capability to a game mode. It connects display metadata and a stable ID to a prefab or component, optional defaults, and operator-controlled mappings.

Content is a contract between your revision and every game mode that consumes it:

```
Addon
└── Revision
    └── Content entry (author definition)
        └── Game-mode content mapping (operator selection/config)
            └── Runtime consumer
```

Mounting a package or installing a launcher bundle makes code and assets available. It does not select an entry, place an entity, put equipment in a job, or enable a system.

## Content types

| Type | Use it for | Primary reference |
| --- | --- | --- |
| `Entity` | A placeable/spawnable world object derived from `BaseEntity` | World prefab |
| `Equipment` | An item players can equip, deploy, drop, or receive in a loadout | Equipment world prefab |
| `System` | A configurable addon component attached when content is enabled | Namespace-qualified component type |
| `Fixture` | A persistent prefab placed and managed with the placement tool | Fixture prefab |

Equipment can use a secondary reference for its first-person view-model prefab. Systems use a C# type name instead of a prefab path. Fixtures require a `.prefab` primary reference.

## Stable IDs and references

The portal assigns an ID to a new content entry when the first draft is saved. The addon editor carries that ID into later revisions. Preserve it when editing an existing capability; deleting and recreating the entry creates a different content identity and can leave game modes with an unassigned mapping.

Treat all of these as stable API:

- the content entry ID;
- the `Primary Reference` and `Secondary Reference`;
- the content type;
- config key names; and
- the display name when an operator uses it to identify a mapping.

The primary reference is relative to the **published asset-package root**. If the package contains:

```
entities/kickable-ball/kickable-ball.prefab
```

enter exactly:

```
entities/kickable-ball/kickable-ball.prefab
```

Do not prepend `Assets`, `addons`, `Code`, a network identifier, or the DXRP addon identifier. The same rule applies to an equipment view-model path.

For a launcher bundle, remember that the selected `Assets/` folder itself is removed from the archive. For an s&box package, use the path as it exists inside the published package. The two paths should match when you maintain both delivery modes.

## System entries

System entries require a namespace-qualified component type such as `MyAddon.Systems.EconomySystem`. A content-managed system is created when its entry is enabled and the package is mounted, and removed when it is disabled.

Derive from `BaseSystem<TConfig>` to receive merged content config:

```csharp
public sealed class EconomySystem : BaseSystem<EconomyConfig>
{
    protected override void OnConfigChanged()
    {
        Log.Info( $"Economy multiplier: {Config.Multiplier}" );
    }
}
```

`Context` exposes the content and addon identities. Addon-wide configuration is separate and should use the stable addon ID:

```csharp
var config = Config.Current.Addon<MyAddonConfig>( MyAddonId );
```

Do not put `[AddonService]` on a content-managed system. `[AddonService]` components are attached independently of the game-mode content selection, so using both mechanisms can create duplicate lifecycle ownership.

## Fixture entries

Fixture entries require a prefab primary reference. DXRP clones the prefab, resolves content config against it, and persists the placed transform in fixture state. Identity is not stored on the spawned object — the content id and config override are passed directly to whatever component on the prefab implements `IFixture`.

When the resolved content config contains values, the placement tool can show an editor prefilled with those values. Placement changes are stored with that fixture and applied after the content base and game-mode config layers.

See the [Fixture](/docs/developers/tutorials/fixture) tutorial for the component, prefab, and config-resolution requirements.

## Entry fields

| Field | Guidance |
| --- | --- |
| **Name** | Human-readable name; keep it recognisable across revisions |
| **Description** | Explain what an operator gets and any dependencies |
| **Type** | `Entity`, `Equipment`, `System`, or `Fixture`; do not change this casually |
| **Primary Reference** | Prefab path or namespace-qualified system component type |
| **Secondary Reference** | Optional equipment view-model prefab |
| **Grouping** | Optional category used by consuming UIs |
| **Icon Path** | Optional menu icon path |
| **World Model / Scale** | Optional presentation override |
| **Base Config** | Valid JSON defaults for the component |

## Add an entry to a revision

While editing a draft revision:

1. open **Content** and select **Add Content**;
2. choose the type;
3. enter the name and operator-facing description;
4. set the exact primary and optional secondary references;
5. add valid base config when the component reads one;
6. save the draft; and
7. attach the draft to a test game mode and select the entry.

The entry is revision-specific. A new revision can add, update, or remove entries, but preserve IDs for existing capabilities and explain any migration in the changelog.

## What operators control

After attaching the addon, an operator can:

- choose which entries the game mode includes;
- override the display label and selection-specific config where supported;
- place equipment into jobs and market items;
- place entities in the game mode’s entity list;
- place and manage fixtures; and
- enable a system without changing the author’s package.

Your addon should not assume that every entry is enabled, purchasable, assigned to a job, or available on every server.

## Updating safely

Before publishing a revision:

- keep the existing entry ID when editing an existing feature;
- keep prefab paths stable or ship a migration;
- add config keys with safe defaults instead of renaming existing keys;
- do not silently change the entry type;
- keep the content available in both package and launcher paths if you support both; and
- test a game mode upgrade from the previous revision.

If a content reference changes, expect operators to review **View Changes**, remap the affected content, and resave their game mode. Tell them exactly what changed in the revision changelog.

Next, read [Configuration](/docs/developers/systems/config-resolution) to make `BaseConfig` useful without hard-coding operator choices.
