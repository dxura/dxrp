---
title: Game Modes
description: Build the jobs, market, content, addons, and configuration assigned to a server.
order: 2
---

**Where:** Open **Game Modes**, then create or select a game mode.

A game mode is the gameplay bundle assigned to one or more servers. It is separate from a [Ruleset](/docs/operators/operations/rulesets), which is the written rules shown to players.

## Core details and configuration

Set the name, description, default job, starting balance, and configuration. The JSON editor can format and validate changes, show a diff, reset to the DXRP baseline, or import a game mode payload.

Use **Save** to persist edits. Unsaved changes remain local to the page.

## Jobs and groups

Create job groups to organise related jobs, then configure each job's:

- name, colour, health, and limits;
- election or vote requirements;
- tags and blocked tags;
- equipment loadout; and
- disconnect, respawn, job-change, and ownership-transfer behaviour.

Set a valid default job before assigning the game mode to a production server.

## Market

Market items connect a display name and price to equipment or entity content. Grouping values place related items together in the in-game market. Configure limits and required or blocked job tags where appropriate.

## Content

Content maps entities and equipment from the base game or installed addons into the game mode. References that no longer exist are shown as unassigned; remap or remove them before saving.

## Addons

Use **Add Addon** to attach an owned or licensed addon and select its revision. You can:

- include only the content the game mode needs;
- remap content after an addon revision changes;
- override addon-wide or content-level configuration; and
- update an attached addon to its latest revision.

### Select a load method

The **Load Method** controls how the server receives the addon’s code/assets:

| Method | Select it when | Required data |
| --- | --- | --- |
| **Package** | The addon points to a published s&box package | Addon S&box Identifier; optional revision S&box Version |
| **Launcher** | The DXRP server launcher should install the revision bundles | Code or Assets package on the selected revision |
| **None** | The addon has no external server package | Nothing; use only for shipped/base content or metadata |

The game-mode editor disables methods the selected addon/revision cannot support. If a revision changes from launcher bundles to an s&box package, review this field instead of assuming the old mapping will switch delivery automatically.

For Package delivery, the server fetches the selected package at runtime. For Launcher delivery, the DXRP launcher downloads and extracts the revision bundles before the server starts. A package can mount successfully while still providing no active gameplay: content entries must also be selected in the game mode.

See [Addons](/docs/operators/addons/addons) for installation, licensing, and revision management. Addon configuration merges in layers; [Configuration](/docs/developers/systems/config-resolution) documents the exact order.

## Apply changes

Saving edits updates the portal definition. Use **Sync Servers** or **Update All** to push the game mode to online servers that use it. Check **View Changes** before syncing a busy production network. A load-method or package-version change should be treated like a deploy: test it on one server, inspect the startup log, then update the rest.

Game-mode access uses `gamemodes.view` and `gamemodes.edit`.
