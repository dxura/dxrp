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

## Apps

The phone only has the apps your game mode includes. Apps come from addons' **App** content (the built-in ones from Base Content): map one on the **Content** tab and open it to set:

- **Installed by default**: every player has the app. Turn it off to sell it instead.
- **Price**: what the app costs when it isn't installed by default. There is no app store yet, so apps that aren't installed by default don't appear for now.

## Market

Market items connect a display name and price to equipment or entity content. Grouping values place related items together in the in-game market. Configure limits and required or blocked job tags where appropriate.

## Content

Content maps entities and equipment from the base game or installed addons into the game mode. References that no longer exist are shown as unassigned; remap or remove them before saving.

## Addons

Use **Add Addon** to attach an owned or licensed addon and select its revision. You can:

- include only the content the game mode needs;
- remap content after an addon revision changes;
- override addon-wide or content-level configuration;
- apply the addon’s suggested jobs, market items, and entity settings; and
- update an attached addon to its latest revision.

### Delivery

The server mounts an addon’s s&box package if, and only if, the addon has an S&box Identifier, using the revision’s S&box Version when one is pinned. There is nothing to choose per game mode. A package can mount successfully while still providing no active gameplay: content entries must also be selected in the game mode.

### Revisions and following the latest

Each addon is pinned to a revision. Changing the revision carries your content mappings across. If the new revision drops or changes the type of content you use, you're asked first, and that content is removed along with its entity, equipment, app, market and job settings.

Tick **Keep Updated** to have an addon follow new revisions on its own. It's available for your own network's addons and official ones (like Base Content), not other networks' addons. When a revision is published:

- if it keeps every content this game mode uses (same content, same type), the game mode moves to it automatically. Servers pick it up on their next restart or when you select **Apply**, never mid-session;
- otherwise the game mode stays on its current revision and the addon shows **Update Needs Review**. Click it to see what would be removed and change revision yourself.

Both outcomes are recorded in the audit log. To roll back, pick the previous revision.

### Suggestions

An addon revision can suggest jobs, market items, and entity settings (limits, health, ownership transfer). When you add an addon that has suggestions, a dialog lets you pick which jobs and market items to add, and whether to add the suggested entities that aren’t in the game mode yet with their suggested limits.

Applying copies them in as ordinary rows you can edit or remove; nothing stays linked to the addon. Entity settings are only applied to rows the apply creates, so your own settings are never overwritten. Suggestions never include ranks: add rank restrictions yourself.

The **Suggestions** column on the Addons tab shows how many suggestions are not applied yet, for example after updating to a revision that suggests a new job. Select it to apply them. Removing an addon removes market items for its content and the rows copied from its suggestions, except a suggested job that has become the default job.

See [Addons](/docs/operators/addons/addons) for installation, licensing, and revision management. Addon configuration merges in layers; [Configuration](/docs/developers/systems/config-resolution) documents the exact order.

## Apply changes

Saving edits updates the portal definition. Use **Sync Servers** or **Update All** to push the game mode to online servers that use it. Check **View Changes** before syncing a busy production network. A package-version change should be treated like a deploy: test it on one server, inspect the startup log, then update the rest.

Game-mode access uses `gamemodes.view` and `gamemodes.edit`.
