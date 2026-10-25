---
title: s&box Packages
description: Create, publish, version, and mount an s&box package from a DXRP game mode.
order: 3
---

An s&box package is a cloud-published revision of an s&box project or asset. DXRP can mount one at runtime when a game-mode addon points at its package identity.

This is different from a DXRP revision. A DXRP revision stores portal metadata, content entries, permissions, configuration, and optional launcher bundles. An s&box package stores the project/package files delivered by s&box.

## The identities involved

Keep these values separate:

| Value | Example | Owned by | Used for |
| --- | --- | --- | --- |
| s&box package identity | `myorg.city-jobs` | s&box | Fetching and mounting the package |
| s&box package revision | `myorg.city-jobs#312160` | s&box | Pinning an exact s&box version |
| DXRP addon identifier | `city-jobs` | DXRP network | Stable portal and code namespace |
| DXRP revision | `Revision 4` | DXRP | Versioning metadata, content, permissions, and delivery choices |

The s&box identity is entered on the DXRP addon’s **S&box Identifier** field as `organization.package`, without `https://sbox.game/` and without `/version/...`.

## When to use a package

Choose runtime package delivery when:

- the addon is already a separately published s&box package;
- the package should be fetched and mounted by the game at runtime;
- the package’s target game and compatibility are controlled on s&box; and
- every server using the game mode can reach the s&box package service.

Choose the [launcher bundle](#launcher-bundles) when the addon is only a DXRP code/assets workspace, when the package cannot be published, or when you explicitly need the DXRP server launcher to patch files before startup.

The two delivery modes are alternatives for a game-mode addon. A revision may contain both kinds of data, but the game mode should select the one it is actually prepared to use.

## Create the s&box project

1. Open the s&box editor and choose **New Project**.
2. Select an **Addon Project** for asset content, or the code-capable project type provided by your installed editor for a runtime package.
3. Set the target game to DXRP (`dxura.rp`) when the wizard offers that option.
4. Choose an organization you control and a stable package identifier.
5. Create the project outside the DXRP `game/` checkout unless you are intentionally working on the core game.
6. Add the code, prefabs, scenes, and other assets under source control.
7. Run the project locally and test a clean editor/session before publishing.

The `.sbproj` file describes the s&box project. It is not uploaded as a DXRP launcher package and it is not the portal addon record. In an asset-focused Addon Project, publish the individual asset from the Asset Browser; do not expect the Addon Project folder itself to become a single published package.

## Publish the package

The exact labels can change with the s&box editor, but the publishing sequence is:

1. Open the project or select its publishable asset in the Asset Browser.
2. Enable publishing if the project or asset is not already configured for it.
3. Choose the organization and package ident. The ident cannot be changed later.
4. Review the publish manifest. Include the files the package needs and resolve missing references.
5. Upload the files and wait for compilation/processing to finish.
6. Add a useful title, description, source/license information, and changelog.
7. Publish a new revision.
8. Set the package’s release/visibility state according to who should be able to fetch it.

For a package that contains code, confirm that the project targets the intended parent game and that the published revision contains the code path. Code-capable addon projects depend on the installed s&box editor version, so trust the publish manifest rather than the folder name alone. Test from a clean project or server; editor-local source is not proof that the cloud package contains it.

See the official [s&box project types](https://sbox.game/dev/doc/getting-started/project-types/), [map publishing guide](https://sbox.game/learn/facepunch/map-publish), and [ProjectPublisher API reference](https://sbox.game/api/Editor.ProjectPublisher) for editor-version-specific publishing details.

## Find the package identity and version

The package page normally shows an address like:

```
https://sbox.game/myorg/city-jobs
```

Use `myorg.city-jobs` as the DXRP **S&box Identifier**.

To pin a version, open the package’s **Versions** page. If the URL ends in:

```
/version/312160
```

enter `312160` in the DXRP revision’s **S&box Version** field. DXRP combines the two values at runtime as:

```
myorg.city-jobs#312160
```

Leave the version blank when the game mode should fetch the package’s current version. Pin a version for production when reproducibility matters, and update the pin deliberately after testing a new package revision.

## Connect the package to DXRP

### 1. Set the addon identity

Open the portal addon under **More → Addons → Edit Details** and enter the s&box `organization.package` identity. Save it before creating the revision.

### 2. Create the DXRP revision

Open **New Revision**, set the optional **S&box Version**, write a changelog, and define the content entries and permissions that the package provides. The portal metadata is what lets a game mode expose a prefab or system to operators; mounting the package alone does not add content to a game mode.

If you are using package delivery only, you do not need to upload DXRP Code or Assets bundles. If you also select the Launcher delivery mode later, the revision must contain the corresponding bundles.

### 3. Publish and attach it

After the revision is tested and published:

1. Open **Game Modes** and edit the target game mode.
2. Add the owned or licensed addon.
3. Select the tested DXRP revision.
4. Set **Load Method** to **Package**.
5. Select the content entries the game mode should expose and add any configuration overrides.
6. Save the game mode and sync the affected servers.

At startup DXRP fetches the selected `organization.package` (or its `#version` form), mounts it once for that game-mode addon, and then continues addon discovery. A package fetch/mount failure is logged and the package’s content will not work; check the server log for the package identity.

## Launcher bundles

Launcher delivery uses the independent packages uploaded on the DXRP revision’s **Launcher** tab:

```
Code/       → Code/Addons/<network>/<addon>/
Assets/     → Assets/addons/<network>/<addon>/
```

The portal zips the selected folder’s contents. The selected folder name is not included in the archive. Upload `Code/` and `Assets/` contents directly; do not select a folder that produces `Code/Code/...` or `game/Code/Addons/...` inside the archive.

The DXRP launcher downloads those packages before it starts the server. In this repository, `dxrp-server.cs` clears the existing addon directories, downloads the packages from the server API, extracts them, and can optionally build the result. A standard s&box launcher does not perform that DXRP patch step.

The portal limits launcher bundles to 50 MB for code and 300 MB for assets. A new revision can reuse an unchanged package, replace it with a new upload, or explicitly clear it.

## Package vs Launcher

| | Package | Launcher |
| --- | --- | --- |
| Source | Published s&box package | DXRP revision Code/Assets bundles |
| DXRP field | Addon S&box Identifier | Uploaded package in the revision |
| Game-mode setting | Load Method → Package | Load Method → Launcher |
| Delivery | Fetch and mount at runtime | Extract before server startup |
| Version pin | Optional `Sbox Version` | DXRP revision number |
| Requires custom DXRP launcher | No | Yes |
| Best for | Published, reusable s&box packages | DXRP-only bundles and controlled server patching |

If an addon has neither a package identity nor a launcher bundle, the game mode cannot load its server code/assets. If it only contributes content that is already shipped with DXRP, select **None**.

## Safe versioning

- Treat the s&box package identity as permanent.
- Treat a package version and a DXRP revision as independent numbers; record both in your changelog.
- Pin production to a tested package version when a package update could change code or assets.
- Keep DXRP content IDs, permission IDs, prefab paths, and config keys stable across revisions.
- Publish a new s&box revision and a new DXRP revision for a breaking change; do not rely on “latest” to make the change reversible.
- Test an upgrade from the previous package and a clean install with the new package.

## Troubleshooting

### The package field says it cannot be mounted

The addon has no S&box Identifier, or the value is not `organization.package`. Set it under **Edit Details**, save, and reopen the revision.

### The package fetch fails

Check the server log for the exact identity, confirm the package is released/visible to the server, and test the unpinned identity first. If the unpinned package works but the pinned form does not, the version number is wrong or unavailable.

### The package mounts but content is missing

Mounting does not select content. Confirm that the DXRP revision contains the content entry, the game mode includes it, the primary reference matches the packaged path, and the server was synced after saving the game mode.

### The launcher cannot see the addon

The launcher only downloads the packages returned by the DXRP server API. Verify the server token, the addon is attached to the game mode, the selected revision has the required bundle, and the selected load method is **Launcher**.
