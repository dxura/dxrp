---
title: Publishing Revisions
description: Package addon code and assets, create drafts, choose delivery, and release compatible DXRP revisions.
order: 1
---

A DXRP revision is an immutable, versioned snapshot of an addon’s delivery and portal metadata. It can contain:

- a DXRP launcher code package;
- a DXRP launcher assets package;
- an optional s&box package version pin;
- addon-wide base configuration;
- content entries and their defaults;
- custom permission declarations; and
- a changelog for operators.

Publishing a revision does not automatically update every game mode. Game modes pin a specific revision and operators choose when to update and sync their servers.

## Before creating the revision

Finish these decisions first:

1. Decide between [Package and Launcher delivery](/docs/developers/publishing/sbox-packages).
2. If using Package delivery, set **S&box Identifier** under **Edit Details** to `organization.package`.
3. Build and test the local s&box project or launcher workspace.
4. Check that content references are relative to the package root.
5. Decide which config keys and permissions are public integration points.

Do not use a draft as a substitute for local compilation. The portal stores and validates metadata, but it cannot tell you whether your code behaves correctly in a running server.

## Create a draft

Open **More → Addons**, select your addon, and choose **New Revision**. If a draft already exists, choose **Continue Draft**.

A draft is visible to your own network for testing and is not the live revision for other licensed networks. Save as often as needed. You can discard a draft only after removing any test game-mode mappings that still pin it.

The revision editor contains:

| Field | What it controls |
| --- | --- |
| **Changelog** | What operators need to know about this revision |
| **S&box Version** | Optional version pin for the addon’s s&box package |
| **Package** | The runtime-package metadata view |
| **Launcher** | Upload/reuse/clear DXRP Code and Assets bundles |
| **Content** | The revision’s entity, equipment, system, or fixture entries |
| **Permissions** | The revision’s custom permission declarations |
| **Config** | Addon-wide base configuration inherited by game modes |

## Package the launcher bundles

The launcher workflow accepts a Code folder and an Assets folder. Select the folders themselves in the revision editor; the portal uploads only their contents.

Use this layout:

```
my-addon/
├── Code/
│   └── ...
└── Assets/
    └── ...
```

The installed paths are:

```
Code/   → Code/Addons/<network identifier>/<addon identifier>/
Assets/ → Assets/addons/<network identifier>/<addon identifier>/
```

Because the wrapper folder is removed during upload, a content reference such as:

```
equipment/radio/w_radio.prefab
```

must point to that path inside the selected `Assets/` folder. Do not add `Assets/`, `addons/`, or the network/addon wrapper to the reference.

The current limits are:

| Package | Limit |
| --- | ---: |
| Code | 50 MB |
| Assets | 300 MB |

The editor zips each selected folder in the browser, uploads it to storage, and finalizes it on the server. Large uploads can take time; wait for both processing steps to finish before closing the dialog.

## Reuse, replace, or clear

When creating a revision from a previous one, each package has an independent choice:

- **Reuse** keeps the previous package path without uploading it again.
- **Upload** replaces that side with the newly selected folder.
- **Clear** removes that package from the new revision.

Use **Clear** intentionally. It is different from selecting no folder: selecting no folder normally means “reuse the previous package” when one exists.

For a package-only addon, it is valid to leave both launcher packages empty. For a launcher addon, clear only when the game mode will no longer select **Launcher** or when the package is genuinely no longer required.

## Configure content, permissions, and defaults

Revision metadata is part of the runtime contract:

- content entry IDs are used by game-mode mappings;
- primary and secondary references point into the published asset package;
- base config becomes the author’s default layer; and
- permission identifiers are granted to ranks by operators.

Keep identifiers and references stable. Add a new entry when the capability is genuinely new; do not silently change an entity into equipment or rename a permission to improve its display text.

The [Content Entries](/docs/developers/publishing/content-entries), [Configuration](/docs/developers/systems/config-resolution), and [Permissions](/docs/developers/systems/permissions) pages describe the individual contracts.

## Choose the delivery mode

| | Package | Launcher | None |
| --- | --- | --- | --- |
| Source | Published s&box package | DXRP Code/Assets bundles | Already shipped or metadata-only |
| Game mode setting | **Package** | **Launcher** | **None** |
| Version | Optional S&box Version | DXRP revision number | Not applicable |
| Server behavior | Fetch and mount at runtime | Patch before startup | No addon files loaded |
| Requirement | Valid S&box Identifier | At least one launcher bundle | No package/bundle needed |

Package delivery uses the selected s&box identity and optional version as `organization.package#version`. Launcher delivery is the path used by the DXRP server launcher to extract files into the repository before starting s&box.

Do not select **Package** for an addon that has no S&box Identifier. Do not select **Launcher** for a revision with no Code or Assets bundle. The game-mode editor disables unsupported choices, but an older mapping may still need to be reviewed after a package change.

## Test the draft end to end

Create a test game mode or use a private one:

1. Add the addon.
2. Select the draft revision.
3. Choose **Package**, **Launcher**, or **None** to match the revision.
4. Select the content entries needed for the test.
5. Apply addon-wide and content-level config overrides.
6. Configure jobs, market items, entities, or fixtures that consume the content.
7. Save the game mode and sync a test server.
8. Restart if using launcher delivery, or wait for runtime package mounting during startup/game-mode updates.

Check all of the following:

- the server can obtain the package or bundle;
- the code compiles and addon services are discovered once;
- every prefab and secondary reference resolves;
- UI and localization assets load on a clean client;
- host-authoritative behavior rejects unauthorized client requests;
- config JSON merges into the expected typed defaults;
- declared permissions appear in rank management;
- existing saved data loads after the update; and
- the previous revision can be upgraded without losing game-mode mappings.

Test both a clean install and an upgrade. A release that works only because an old package is still present locally is not ready.

## Publish the revision

Before publishing, write a changelog that answers:

- What changed?
- Does the operator need to select a different load method?
- Did the s&box package identity or version requirement change?
- Were content or permission IDs added, removed, or renamed?
- Do existing game modes need remapping?
- Is a server restart or resync required?

Select **Publish** and confirm. The revision becomes the live version for consumers who update to it. Published revisions cannot be edited in place; create another revision for a fix or follow-up.

Publishing also does not move existing game modes automatically. After release, operators must open the game mode, select the new revision, review content mappings and overrides, save, and sync the affected servers.

## Update safely

Treat the following as public API:

- DXRP addon identifier;
- s&box package identity;
- content entry IDs;
- content references;
- permission IDs;
- config keys and their meaning; and
- persisted data keys.

Prefer additive changes with defaults. If a breaking change is unavoidable, publish a new content entry or a new package identity and explain the migration in the changelog.

Only the newest draft is editable for new uploads; historical published revisions remain available for game modes that still pin them. Keep the old revision available until operators have migrated and verified production.

## Release checklist

- [ ] The addon identifier and s&box package identity are correct.
- [ ] The chosen load method has a matching package or bundle.
- [ ] No secrets, tokens, local saves, caches, or unrelated files are included.
- [ ] Code and asset paths are flat relative to their package roots.
- [ ] Every content reference matches the packaged path exactly.
- [ ] Content entry IDs and permission IDs were preserved where intended.
- [ ] Base config is valid JSON with useful code defaults.
- [ ] The changelog calls out operator action and restart/sync requirements.
- [ ] The draft was tested through a game mode on a clean server/client.
- [ ] An upgrade from the previous revision was tested.
- [ ] The game mode was checked after selecting the new revision.

For visibility, source access, pricing, and entitlement behavior, continue to [Pricing & Licensing](/docs/developers/publishing/monetization).
