---
title: Addons
description: Manage owned and licensed addons, revisions, packages, content, permissions, and network access.
order: 1
---

**Where:** Open **More → Addons**.

The tab has two distinct views:

- addons your network owns and can publish; and
- **Licensed Addons** your network is entitled to use.

To use either kind on a server, attach it under [Game Modes](/docs/operators/operations/game-modes).

## Install or license an addon

Browse the public addon marketplace from the portal. A public free addon can be claimed directly. Paid or private addons require an entitlement from the author. After access is granted, the addon appears under **Licensed Addons**.

Licensing makes the addon available to the network; it does not automatically add it to every game mode.

## Manage an owned addon

The details page controls its name, description, visibility, pricing, source visibility, thumbnail, screenshots, video, and public marketplace page. Identifiers are permanent references, so choose them carefully.

Use **Grant Access** to entitle another network to a paid or private addon. Regranting an existing entitlement reactivates it instead of creating a duplicate.

## Revisions

Create a revision to publish code, assets, content, and permissions:

1. start **New Revision**;
2. upload new code or asset packages, or explicitly reuse the previous package;
3. add or edit content entries and permission declarations;
4. save a draft while testing;
5. review the changelog and package choices; and
6. publish when the revision is ready for consumers.

Publishing makes that revision live. Removing or changing a published content or permission identifier can break game-mode mappings and existing rank grants.

Code packages can be up to 50 MB and asset packages up to 300 MB. A revision may instead point at a published s&box package through its S&box Identifier and optional version.

The two delivery choices are selected per game-mode addon mapping:

| Load method | Uses | Server behavior |
| --- | --- | --- |
| **Package** | Addon S&box Identifier and revision S&box Version | Fetches and mounts `organization.package` or `organization.package#version` at runtime |
| **Launcher** | Revision Code/Assets bundles | The DXRP launcher extracts the bundles before startup |
| **None** | No addon package is required | The addon is not loaded as an external server package |

The authoring workflow is covered in [Creating an Addon](/docs/developers/creating-addons), [Publishing Revisions](/docs/developers/publishing/publishing-addons), and [s&box Packages](/docs/developers/publishing/sbox-packages).

## Update a server's addon

Publishing alone does not change a game mode pinned to an older revision. Open **Game Modes → Addons**, review **View Changes**, remap any changed content, confirm the load method still has a matching package/bundle, select the new revision, save, and sync the affected servers.

Addon access uses `addons.view` and `addons.edit`.
