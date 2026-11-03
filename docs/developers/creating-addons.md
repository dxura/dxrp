---
title: Creating an Addon
description: Create a DXRP addon, describe it in a .dxaddon definition, connect it to an s&box package, and test it locally.
order: 2
---

This is the practical starting point for a DXRP addon. It explains what the portal record is, where the code and assets live, and how the addon eventually reaches a game mode and server.

If you have not created the local s&box project yet, start with [Creating a Project](/docs/developers/creating-projects). If you already have a published s&box package, read [s&box Packages](/docs/developers/publishing/sbox-packages) before creating the portal revision.

## Understand the four layers

An addon is not one upload. It is a chain of related records and files:

```
Local s&box project (with a .dxaddon definition)
    └── published s&box package
            ↓
DXRP portal addon
    └── DXRP revision
            ↓
DXRP game-mode mapping
            ↓
Running server
```

| Layer | What it owns | What it does not do |
| --- | --- | --- |
| s&box project | `.sbproj`, source files, assets, target game, package identity | Create the DXRP portal record |
| s&box package | Cloud-delivered package revisions | Define DXRP content mappings or permissions |
| DXRP addon | Stable portal identifier, listing, licensing, package identity | Load code by itself |
| DXRP revision | Changelog, s&box version pin, config, content, permissions, suggestions | Automatically attach itself to every game mode |
| Game-mode mapping | Selected revision, content, applied suggestions and overrides | Publish an s&box package |

Creating a portal addon without a revision is only creating metadata. Publishing a revision without attaching it to a game mode does not make its content active on a server.

## Before you start

You need:

- a DXRP network where your rank has addon edit access;
- the s&box editor and a local project that targets DXRP when you are using DXRP-specific code;
- a source-control workspace for the addon; and
- a permanent DXRP addon identifier.

DXRP addon identifiers must use lowercase letters, numbers, and hyphens, begin with a letter or number, and be at most 64 characters. They are network-scoped in the portal. Choose one that will still make sense in namespaces, permission IDs, and server paths; it cannot be changed after creation.

## Step 1: understand how the addon is delivered

Addons reach servers only as s&box packages. A game mode mounts an addon if, and only if, the addon has an **S&box Identifier**. The revision's optional **S&box Version** pins which published package version is mounted.

| Addon has | What the server does |
| --- | --- |
| An S&box Identifier | Mounts `organization.package`, or `organization.package#version` when the revision pins a version |
| No S&box Identifier | Mounts nothing. The addon only carries content mappings and config, e.g. content already shipped in the DXRP game |

DXRP does not upload or install code or asset bundles; publish your code and assets as an s&box package.

## Step 2: create the portal addon

Open **More → Addons → Create New Addon** in the DXRP portal.

Enter:

| Field | Guidance |
| --- | --- |
| **Addon Name** | A human-readable name shown to operators and marketplace users |
| **Addon Identifier** | Lowercase, stable, machine-readable identifier |
| **Description** | A short explanation of the feature; keep the longer guide for the listing’s About text |

The new addon starts private and free. That is a good state for development. Do not make it public or paid until a clean revision has been tested.

Select **Edit Details** after creation and set **S&box Identifier** to the package identity, such as `myorg.city-jobs`. Do not enter the full URL or a version suffix. See [s&box Packages](/docs/developers/publishing/sbox-packages) for version pinning.

## Step 3: organise the local project

Keep the normal s&box project layout and publish through the editor. Keep your assets under a folder unique to your addon, for example:

```
Assets/addons/myorg/city-jobs/
├── city-jobs.dxaddon
├── entities/
└── equipment/
```

Content references are paths as they exist inside the published package, e.g. `addons/myorg/city-jobs/entities/city-sign/city-sign.prefab`.

### The addon definition (`.dxaddon`)

Describe the addon in a **DXRP Addon** resource (`.dxaddon`, class `Dxura.RP.Game.Addons.AddonDefinition`). Create one from the Asset Browser and edit it in the inspector. It is the single source of truth for the addon's definition:

- in the editor, every mounted `.dxaddon` is injected into the local game mode (see [Local addon definitions](#local-addon-definitions)); and
- on the portal, **Import Definition** reads the same file to fill in the next revision (see [Step 6](#step-6-define-content-permissions-and-suggestions)).

| Property | Purpose |
| --- | --- |
| `AddonId` | Your portal addon id |
| `Name`, `Description` | Shown when injected locally |
| `InjectInEditor` | Turn off to stop the editor injecting it, e.g. to test the published revision instead |
| `GlobalBaseConfig` | The revision's global base config, as JSON text |
| `Contents` | Content entries, the same `CreateAddonContentDto` the portal saves: `Id`, `Type` (Entity, Equipment, System, Fixture), `Name`, `Description`, `PrimaryReference`, `SecondaryReference`, `Grouping`, `IconPath`, `WorldModelPath`, `WorldModelScale`, `BaseConfig` (JSON text) |
| `Permissions` | `Identifier` (the suffix only), `Name`, `Description`, `Category` |
| `Suggestions.Jobs` | Suggested jobs: `Key`, `Name`, `Description`, `Color` (packed `0xRRGGBB`), `Model`, `Clothes`, `JobTags`, `Salary`, `Health`, `MaxCount`, `Selectable`, `Demotable`, `VoteRequired`, `IncludeDefaultEquipment`, `Equipment` (ids of Equipment content entries) |
| `Suggestions.Entities` | Suggested entity settings per Entity content: `Content` (content id), `Limit`, `HealthEnabled`, `HealthAmount`, `DestroyOnDisconnect`, `DestroyOnJobChange`, `AllowOwnershipTransfer` |
| `Suggestions.MarketItems` | Suggested market listings: `Key`, `Content` (an Entity or Equipment content id), `Cost`, `Grouping`, `SortOrder`, `Color`, `Quantity`, `WhitelistJobs` (suggested job keys), `WhitelistJobTags` |

The content, permission and suggestion types are the platform's own API DTOs, synced into the game, so the file is exactly what a revision stores. The portal validates suggestions when you save a revision: every `Content` must be one of the revision's contents of a fitting type, keys must be unique, and market `WhitelistJobs` must name suggested jobs.

A content `Id` is generated when you add an entry. Keep it across revisions: game modes bind to it. Job `Key`s are how the portal tells which suggested jobs a game mode already has, so keep those stable too.

The official brewing addon (`addons/official/brewing/brewing.dxaddon`) is a complete example, with suggested entity settings and market items.

## Step 4: add an entry point only when you need one

An addon that only provides prefab components does not need a global bootstrap class. For background behaviour, state, event subscriptions, or a scene-wide integration, use an `[AddonService]` component:

```csharp
using Dxura.RP.Game.Addons;

namespace Dxura.RP.Game.Addons.MyNetwork.CityJobs;

[AddonService]
public sealed class CityJobsService : Component
{
    protected override void OnStart()
    {
        Log.Info( "City Jobs addon loaded" );
    }
}
```

`AddonSystemRegistry` discovers concrete components carrying `[AddonService]` and creates one on the core systems object (`GameManager.SystemsGameObject`) when core systems start, and again for any type that appears when an addon package mounts later. There is no registration list and no `IAddonService` interface.

A service added by a package that mounts after startup misses the startup events. Read [Lifecycle](/docs/developers/hooks/events/lifecycle) before relying on `OnGameStartupCompleted` or the first `OnGameModeUpdated`.

Use a service for long-lived coordination. Do not mark every entity, equipment component, or UI provider as an addon service; those are instantiated by their own prefab or registry lifecycle.

Namespace code with your network and addon identity, for example:

```
Dxura.RP.Game.Addons.MyNetwork.CityJobs
```

This reduces collisions in the shared compiled game and makes logs and stack traces easier to read.

## Step 5: add the feature

Choose the smallest feature path that matches the addon:

- [Entity](/docs/developers/tutorials/entity) for a spawnable world object;
- [Equipment](/docs/developers/tutorials/equipment) for an item players can equip or buy;
- [Service](/docs/developers/tutorials/service) for a persistent scene service;
- [Command](/docs/developers/tutorials/command) for a discoverable chat command;
- [Tab Menu](/docs/developers/tutorials/tab-menu) for an in-game page or action;
- [Configuration](/docs/developers/systems/config-resolution) for operator-tunable values;
- [Permissions](/docs/developers/systems/permissions) for staff or player access; and
- [Persistence](/docs/developers/systems/persistence) for data that survives restarts.

Before writing anything networked or anything that touches money, read [Networking](/docs/developers/systems/networking) and [Security](/docs/developers/systems/security).

An addon can be code-only. Content entries, custom permissions, persistent data, and UI extensions are optional.

## Step 6: define content, permissions and suggestions

Content, permissions and suggestions are authored on the addon, then snapshotted into a revision. The easiest way is to keep them in your `.dxaddon` and select **Import Definition** on the addon page: it fills the **Content**, **Config**, **Permissions** and **Suggestions** tabs from the file. Existing permission ids are kept where the identifiers match, and you are warned if the file's `AddonId` belongs to a different addon. Nothing is saved until you save a revision, so reloading the page undoes a wrong import.

You can also edit the tabs by hand. For a prefab-backed feature, a content entry needs the exact packaged reference:

| Feature | Type | Primary reference |
| --- | --- | --- |
| Spawnable world object | `Entity` | `entities/city-sign/city-sign.prefab` |
| Equippable item | `Equipment` | `equipment/radio/w_radio.prefab` |
| Background/configurable system | `System` | Namespace-qualified component type |
| Persistent placed object | `Fixture` | A `.prefab` path |

The content entry does not load the package and does not grant an item automatically. It gives a game-mode owner a stable capability to select, configure, and map.

### Suggestions

An addon cannot own jobs or market items: those belong to each game mode. Instead, a revision can **suggest** them. Suggestions are jobs, market items, and entity settings (limits, health, ownership transfer) that a game-mode owner can choose to apply. Applying copies them into the game mode as ordinary rows the owner can edit; nothing stays linked to the addon. Suggestions never include ranks, because ranks belong to each network.

The **Suggestions** tab shows the suggestions as JSON (`Jobs`, `Entities`, `MarketItems`). It is normally filled by **Import Definition** from the `.dxaddon` `Jobs` list and each content entry's `Entity` and `Market` blocks.

### Permissions

Declare custom permissions only for capabilities operators need to grant. Permission identifiers are prefixed with the addon identifier and should be treated as permanent. Read [Content Entries](/docs/developers/publishing/content-entries) and [Permissions](/docs/developers/systems/permissions) before renaming or removing either.

## Step 7: test locally

### Run DXRP in the editor

You do not need to publish and restart a dedicated server for every change. With your addon project open in the s&box editor (targeting `dxura.rp`):

1. In the Asset Browser, open `scenes/game.scene` from the `dxura.rp` package, not your project's own scene.
2. Press **Play**.

DXRP boots as a local host: the map loads, your code is compiled in, and your `[AddonService]` components attach and run. Saving a source file hot-reloads it.

Pressing Play without opening that scene first starts your project's `StartupScene`, which for a new project is the empty `scenes/minimal.scene` template, so nothing from DXRP runs. See [Creating a Project](/docs/developers/creating-projects#run-dxrp-from-your-project).

What this local run gives you, and what it does not:

| | Local editor run |
| --- | --- |
| Game mode | The public default game mode. With no server authorization key, nothing is fetched from your network |
| Your code | Loaded: services, commands, statuses, tab menu sections, and other type-discovered code run |
| Your content entries | Mapped from your `.dxaddon` definition (below), with its suggested entities, market items and jobs applied |
| Addon and content config | The `GlobalBaseConfig` in your `.dxaddon`, then your C# defaults; no operator override |
| Permissions | The local host gets an "Owner" rank with every permission. Use a second client to test a refusal |
| Store | The in-memory mock: works, but resets when you stop |
| Machines | One host, plus any extra local client you start from the editor |

So use it for the fast loop ("does my service attach", "does my command run", "does my UI open"), and use a test server with a draft revision (Step 8) for portal config, real ranks, and the published package.

### Local addon definitions

In the editor only, the host injects every mounted `.dxaddon` into whatever game mode it loads. There is nothing to register: a definition in your project or in any mounted package is picked up just by existing. Each one adds the addon, its content entries, an entity or equipment row per entry, and its suggestions: entity settings, market items (with job whitelists resolved) and jobs.

A definition replaces any addon with the same `AddonId` already in the game mode, so it also works as a local override of a published revision. Set `InjectInEditor` off to test the published revision instead. Dedicated servers and published builds never inject definitions.

### Checklist

Test the feature in the same shape in which it will ship:

1. Build the local project and fix compile errors before publishing the package.
2. Check that prefabs open and all asset references resolve.
3. Test both host and client behaviour for shared state.
4. Confirm an addon service is created once per scene, not once per player.
5. Test missing or default configuration and invalid operator overrides.
6. Test permission checks on the host; client UI checks are not authorization.
7. Test a clean project/server with no stale addon files.
8. Record the exact package identity, DXRP revision, content IDs, and config changes in the test notes.

The editor can see local source that was never published. A clean-install test is the fastest way to catch a wrong package path or missing reference.

## Step 8: create and test a revision

Publish your s&box package first. Then open the addon page, select **Import Definition** to bring in your latest `.dxaddon`, and open **New Revision**. Add a changelog and, optionally, an s&box version to pin the published package version. Save the revision as a draft first.

Attach the draft to a test game mode. If the revision has suggestions, the portal offers to apply them. Select the content the test game mode should use, apply config overrides, save, and sync the test server. Then follow [Publishing Revisions](/docs/developers/publishing/publishing-addons) for the release checklist.

## What to record for operators

Your revision changelog and addon About text should state:

- the s&box package identity and whether a version pin is required;
- which suggestions are worth applying and which are optional;
- which content entries are safe to enable;
- which config keys are available and their defaults;
- which permissions must be granted;
- whether an update requires remapping or a restart; and
- any server-side or platform dependency.

This turns a working addon into an operable addon. Operators should not need to read your source to know how to install or update it.
