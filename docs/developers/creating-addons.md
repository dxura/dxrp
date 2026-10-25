---
title: Creating an Addon
description: Create a DXRP addon, connect it to a local s&box project, choose a delivery mode, and test it locally.
order: 2
---

This is the practical starting point for a DXRP addon. It explains what the portal record is, where the code and assets live, and how the addon eventually reaches a game mode and server.

If you have not created the local s&box project yet, start with [Creating a Project](/docs/developers/creating-projects). If you already have a published s&box package, read [s&box Packages](/docs/developers/publishing/sbox-packages) before creating the portal revision.

## Understand the four layers

An addon is not one upload. It is a chain of related records and files:

```
Local s&box project
    ├── published s&box package (optional)
    └── Code/ + Assets/ launcher workspace (optional)
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
| DXRP revision | Changelog, package references, config, content, permissions | Automatically attach itself to every game mode |
| Game-mode mapping | Selected revision, load method, content and overrides | Publish an s&box package |

Creating a portal addon without a revision is only creating metadata. Publishing a revision without attaching it to a game mode does not make its content active on a server.

## Before you start

You need:

- a DXRP network where your rank has addon edit access;
- the s&box editor and a local project that targets DXRP when you are using DXRP-specific code;
- a source-control workspace for the addon; and
- a permanent DXRP addon identifier.

DXRP addon identifiers must use lowercase letters, numbers, and hyphens, begin with a letter or number, and be at most 64 characters. They are network-scoped in the portal. Choose one that will still make sense in namespaces, permission IDs, and server paths; it cannot be changed after creation.

## Step 1: decide how the addon will be delivered

Choose before you package the first release:

| Choose | When | What you need |
| --- | --- | --- |
| **Package** | You maintain a standalone s&box package and want runtime mounting | An s&box `organization.package` identity, plus a DXRP revision |
| **Launcher** | You want the DXRP launcher to patch code/assets before startup | DXRP `Code/` and/or `Assets/` bundle folders in each revision |
| **None** | The addon is already shipped in the DXRP game or contains metadata only | No server-side package or bundle |

Package delivery is usually the cleanest option for a separately published s&box addon. Launcher delivery is useful for a DXRP-only bundle and is the path supported by the `dxrp-server.cs` workflow in this repository. The selected game-mode load method must match the delivery data you publish.

## Step 2: create the portal addon

Open **More → Addons → Create New Addon** in the DXRP portal.

Enter:

| Field | Guidance |
| --- | --- |
| **Addon Name** | A human-readable name shown to operators and marketplace users |
| **Addon Identifier** | Lowercase, stable, machine-readable identifier |
| **Description** | A short explanation of the feature; keep the longer guide for the listing’s About text |

The new addon starts private and free. That is a good state for development. Do not make it public or paid until a clean revision has been tested.

If using Package delivery, select **Edit Details** after creation and set **S&box Identifier** to the package identity, such as `myorg.city-jobs`. Do not enter the full URL or a version suffix. See [s&box Packages](/docs/developers/publishing/sbox-packages) for version pinning.

## Step 3: organise the local workspace

For a launcher bundle, keep the upload roots flat:

```
my-addon/
├── Code/
│   ├── MyAddonService.cs
│   └── ...
└── Assets/
    ├── entities/
    ├── equipment/
    └── ...
```

Put C# and Razor source directly under `Code/`. Put prefabs, models, sounds, textures, scenes, and other s&box assets directly under `Assets/`.

The portal uploads the **contents** of the selected folder. It removes the selected folder itself, so a file at `Assets/entities/tool/tool.prefab` is packaged as `entities/tool/tool.prefab` and should be entered in the content reference exactly that way.

Do not put any of these wrapper paths inside the selected upload folder:

```
game/Code/Addons/<network>/<addon>/...
Code/Addons/<network>/<addon>/...
Assets/addons/<network>/<addon>/...
```

Those are installation paths created by the launcher. Including them yourself causes doubled paths and missing prefabs.

For an s&box package project, keep the normal project layout and publish through the editor. The s&box project wrapper and editor caches are not substitutes for the DXRP launcher package folders.

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

`AddonServiceRegistry` discovers concrete components carrying `[AddonService]` and creates one on the `GameManager` root after the scene loads. There is no registration list and no `IAddonService` interface.

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

An addon can be code-only. Content entries, custom permissions, persistent data, and UI extensions are optional.

## Step 6: define content and permissions

Content and permissions are authored on the addon, then snapshotted into a revision.

For a prefab-backed feature, open the addon’s **Content** tab and create an entry with the exact packaged reference:

| Feature | Type | Primary reference |
| --- | --- | --- |
| Spawnable world object | `Entity` | `entities/city-sign/city-sign.prefab` |
| Equippable item | `Equipment` | `equipment/radio/w_radio.prefab` |
| Background/configurable system | `System` | Namespace-qualified component type |
| Persistent placed object | `Fixture` | A `.prefab` path |

The content entry does not load the package and does not grant an item automatically. It gives a game-mode owner a stable capability to select, configure, and map.

Declare custom permissions only for capabilities operators need to grant. Permission identifiers are prefixed with the addon identifier and should be treated as permanent. Read [Content Entries](/docs/developers/publishing/content-entries) and [Permissions](/docs/developers/systems/permissions) before renaming or removing either.

## Step 7: test locally

Test the feature in the same shape in which it will ship:

1. Build the local project and fix compile errors before portal upload.
2. Check that prefabs open and all asset references resolve.
3. Test both host and client behaviour for shared state.
4. Confirm an addon service is created once per scene, not once per player.
5. Test missing or default configuration and invalid operator overrides.
6. Test permission checks on the host; client UI checks are not authorization.
7. Test a clean project/server with no stale addon files.
8. Record the exact package identity, DXRP revision, content IDs, and config changes in the test notes.

The editor can see local source that was never published. A clean-install test is the fastest way to catch a wrong package path or missing reference.

## Step 8: create and test a revision

Open **New Revision** from the addon page. Add a changelog, review the inherited content/config/permissions, set an optional s&box version, and upload or reuse the appropriate packages. Save the revision as a draft first.

Attach the draft to a test game mode and choose its load method:

- **Package** mounts the published s&box package named by the addon’s S&box Identifier;
- **Launcher** installs the revision’s Code/Assets bundles before startup; or
- **None** leaves the addon unloaded when no server package is required.

Select the content the test game mode should use, apply config overrides, save, and sync the test server. Then follow [Publishing Revisions](/docs/developers/publishing/publishing-addons) for the release checklist.

## What to record for operators

Your revision changelog and addon About text should state:

- which load method to select;
- the s&box package identity and whether a version pin is required;
- which content entries are safe to enable;
- which config keys are available and their defaults;
- which permissions must be granted;
- whether an update requires remapping or a restart; and
- any server-side or platform dependency.

This turns a working addon into an operable addon. Operators should not need to read your source to know how to install or update it.
