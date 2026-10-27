---
title: Creating a Project
description: Choose the right s&box project type, open DXRP locally, and prepare a project for package publishing.
order: 1
---

There are two different things people call a “project” in DXRP:

- an **s&box project**, represented by a `.sbproj` file and opened by the s&box editor; and
- a **DXRP portal addon**, which owns revisions, content entries, permissions, licensing, and game-mode access.

You normally create the s&box project first, then create the portal addon when you are ready to distribute a feature through DXRP. They are related, but creating one does not create the other.

## Choose a project type

| You want to build | Start with | Result |
| --- | --- | --- |
| DXRP itself | The checked-out `game/rp.sbproj` | The DXRP game project, with its code, scenes, UI, and assets |
| A reusable DXRP asset feature | An **Addon Project** targeted at `dxura.rp` | Assets published individually and referenced by a DXRP game mode |
| A runtime addon with code | A code-capable targeted project when your s&box build supports it, or a launcher workspace | A published code package or a DXRP Code/Assets bundle |
| A standalone map or asset package | The appropriate s&box asset/project template | A package that can be referenced by a map or registered in the DXRP portal |
| A DXRP launcher addon | A normal local workspace with `Code/` and `Assets/` folders | Code and assets that can be uploaded to a DXRP revision and installed by the launcher |

The s&box editor’s project wizard and current documentation are the source of truth for which templates are available in your installed build. If the wizard offers **Target Game**, choose DXRP (`dxura.rp`) for a package intended to run inside DXRP.

Project capabilities are editor-version dependent. The official Addon Project guide describes Addon Projects as asset-focused and says they are published from the Asset Browser; newer targeted-addon work can also expose code-targeting features. If your installed wizard does not allow the code layout you need, use the project type it provides for the target game or use DXRP Launcher delivery, and confirm the publish manifest before building the DXRP portal revision.

An Addon Project is not the same as a DXRP portal addon. The former describes an s&box package; the latter is the delivery and configuration record used by DXRP networks.

## Open DXRP locally

For work on the DXRP game, clone or open the repository and open:

```
game/rp.sbproj
```

The project currently identifies itself as:

```json
{
  "Type": "game",
  "Org": "dxura",
  "Ident": "rp"
}
```

Open the `.sbproj` directly, or launch `sbox-dev.exe` with the project path. A package project may also include a target/parent package in its metadata; use the project wizard or settings rather than copying a guessed field into the file. The game project owns the DXRP code and built-in assets; do not copy addon code into the core project when you are developing a separately delivered addon.

For a package project, create a separate directory beside the game checkout. Keeping it outside `game/` makes it obvious which files belong to the addon and prevents accidental edits to core DXRP code.

## A minimal project layout

An s&box project commonly looks like this:

```
my-project/
├── my-project.sbproj
├── Assets/
│   ├── scenes/
│   ├── models/
│   └── ...
├── Code/
│   └── ...
├── Editor/                 # optional editor-only code
└── ProjectSettings/        # generated or editor-managed settings
```

For a DXRP launcher revision, the portal does not need the project wrapper. It expects the contents of two selected folders:

```
my-addon/
├── Code/                   # C# and Razor source
└── Assets/                 # prefabs and other s&box assets
```

For a published s&box package, publish the supported asset/project from the s&box editor or Asset Browser so s&box can calculate compiled assets and references. Asset-focused Addon Projects publish their individual assets rather than the project folder as one package. Do not manually zip `.sbproj`, `bin`, `.sbox`, or editor caches and call that an s&box package.

## Project identity

An s&box package identity has two parts:

```
organization.package
```

For example, `dxura.rp` is the DXRP game package. The corresponding web URL is usually:

```
https://sbox.game/dxura/rp
```

Choose the organization and package identifier carefully. Treat them as permanent public API: changing them creates a different package and requires every consumer to update its reference.

The `ParentPackage` or target-game setting describes the package the addon is built for. It is not a DXRP portal addon identifier and it does not grant a network access to your package.

## Recommended local workflow

1. Install the current s&box editor and restart it after installing an update.
2. Create or open the project from its `.sbproj` file.
3. Set the target game/parent package before writing DXRP-specific code.
4. Put source-controlled code and assets in `Code/` and `Assets/`.
5. Keep generated folders such as `.sbox/`, `bin/`, and `obj/` out of source control.
6. Run DXRP locally with your project loaded (see [below](#run-dxrp-from-your-project)) and fix compile, asset, prefab, and network errors before publishing.
7. Publish or upload a test revision only after checking the exact paths used by your content entries.

Hotload makes code iteration fast, but it does not replace a clean-install test. A local editor can resolve files that were never included in the package or that exist only in your cache.

## Run DXRP from your project

A new project's `.sbproj` sets `"StartupScene": "scenes/minimal.scene"`, the empty s&box template. Pressing **Play** in your own project therefore starts an empty scene, not DXRP.

The scene that boots DXRP belongs to the parent package. To run DXRP with your addon code loaded, open `scenes/game.scene` from `dxura.rp` in the Asset Browser, then press **Play**. No server authorization key is needed; DXRP starts as a local host on the public default game mode.

[Creating an Addon](/docs/developers/creating-addons) (Step 7) lists what this local run covers and what still needs a test server.

## Project-to-addon flow

Use this sequence when building a distributable DXRP feature:

```
s&box project
    ↓ local test
s&box package (optional)
    ↓ bind author.package in DXRP
DXRP portal addon
    ↓ create revision
game-mode addon mapping
    ↓ choose Package or Launcher
running server
```

Read [Creating an Addon](/docs/developers/creating-addons) next for the DXRP-specific setup, then [s&box Packages](/docs/developers/publishing/sbox-packages) if you are using runtime package mounting.

## Common mistakes

- **Creating only a portal addon:** the portal record has no executable code until a revision contains a launcher bundle or points to an s&box package.
- **Using the DXRP addon identifier as the s&box package identifier:** they are separate identifiers and can have different values.
- **Opening the wrong project:** open `game/rp.sbproj` for core DXRP work; open your own `.sbproj` for an external addon.
- **Publishing generated files:** `.sbox`, `bin`, `obj`, logs, local saves, and secrets do not belong in a package.
- **Using a local path as a package reference:** a local project path is useful for testing only; servers need a published package identity or a DXRP launcher revision.

For the platform-side package workflow, see the official [s&box project types](https://sbox.game/dev/doc/getting-started/project-types/) and [dedicated server documentation](https://sbox.game/dev/doc/networking-multiplayer/dedicated-servers/).
