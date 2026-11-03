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

The details page controls its name, description, visibility, pricing, thumbnail, screenshots, video, and public marketplace page. Identifiers are permanent references, so choose them carefully.

Use **Grant Access** to entitle another network to a paid or private addon. Regranting an existing entitlement reactivates it instead of creating a duplicate.

## Revisions

Create a revision to publish content, config, permissions, and suggestions:

1. select **Import Definition** to fill the addon page from the addon’s `.dxaddon` file, or edit the **Content**, **Config**, **Permissions**, and **Suggestions** tabs by hand;
2. start **New Revision**;
3. write a changelog and, optionally, pin an **S&box Version**;
4. save a draft while testing; and
5. publish when the revision is ready for consumers.

Publishing makes that revision live. Removing or changing a published content or permission identifier can break game-mode mappings and existing rank grants.

Addon code and assets are delivered only as s&box packages. A game mode mounts the addon’s package if, and only if, the addon has an **S&box Identifier**: it fetches `organization.package`, or `organization.package#version` when the revision pins a version, at runtime. An addon without an identifier mounts nothing and only contributes content mappings and config.

A revision can also **suggest** jobs, market items, and entity settings. Game-mode owners choose which to apply; see [Game Modes](/docs/operators/operations/game-modes#suggestions).

The authoring workflow is covered in [Creating an Addon](/docs/developers/creating-addons), [Publishing Revisions](/docs/developers/publishing/publishing-addons), and [s&box Packages](/docs/developers/publishing/sbox-packages).

## Update a server's addon

Publishing alone does not change a game mode pinned to an older revision. Open **Game Modes → Addons**, review **View Changes**, remap any changed content, select the new revision, apply any new suggestions you want, save, and sync the affected servers.

Addon access uses `addons.view` and `addons.edit`.
