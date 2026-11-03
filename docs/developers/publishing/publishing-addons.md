---
title: Publishing Revisions
description: Import an addon definition, create drafts, pin an s&box package version, and release compatible DXRP revisions.
order: 1
---

A DXRP revision is an immutable, versioned snapshot of an addon’s portal metadata. It can contain:

- an optional s&box package version pin;
- addon-wide base configuration;
- content entries and their defaults;
- custom permission declarations;
- suggested jobs, market items, and entity settings; and
- a changelog for operators.

Publishing a revision does not automatically update every game mode. Game modes pin a specific revision and operators choose when to update and sync their servers.

## Before creating the revision

Finish these decisions first:

1. Publish your code and assets as an [s&box package](/docs/developers/publishing/sbox-packages). DXRP delivers addons only as s&box packages.
2. Set **S&box Identifier** under **Edit Details** to `organization.package`.
3. Build and test the local s&box project, including its `.dxaddon` definition.
4. Check that content references are relative to the package root.
5. Decide which config keys and permissions are public integration points.

Do not use a draft as a substitute for local compilation. The portal stores and validates metadata, but it cannot tell you whether your code behaves correctly in a running server.

## Create a draft

Open **More → Addons** and select your addon. Select **Import Definition** and pick your `.dxaddon` file to fill the **Content**, **Config**, **Permissions** and **Suggestions** tabs from it (see [Creating an Addon](/docs/developers/creating-addons#the-addon-definition-dxaddon)). Nothing is saved until the revision is saved. Then choose **New Revision**. If a draft already exists, choose **Continue Draft**.

A draft is visible to your own network for testing and is not the live revision for other licensed networks. Save as often as needed. You can discard a draft only after removing any test game-mode mappings that still pin it.

The revision dialog asks for:

| Field | What it controls |
| --- | --- |
| **Changelog** | What operators need to know about this revision |
| **S&box Version** | Optional version pin for the addon’s s&box package |

The rest of the revision is taken from the addon page’s tabs at the moment you save:

| Tab | What it controls |
| --- | --- |
| **Content** | The revision’s entity, equipment, system, or fixture entries |
| **Config** | Addon-wide base configuration inherited by game modes |
| **Permissions** | The revision’s custom permission declarations |
| **Suggestions** | Jobs, market items, and entity settings a game mode can choose to apply |

## Configure content, permissions, and defaults

Revision metadata is part of the runtime contract:

- content entry IDs are used by game-mode mappings;
- primary and secondary references point into the published s&box package;
- base config becomes the author’s default layer; and
- permission identifiers are granted to ranks by operators; and
- suggested job keys let the portal tell which suggestions a game mode already has.

Keep identifiers and references stable. Add a new entry when the capability is genuinely new; do not silently change an entity into equipment or rename a permission to improve its display text.

The [Content Entries](/docs/developers/publishing/content-entries), [Configuration](/docs/developers/systems/config-resolution), and [Permissions](/docs/developers/systems/permissions) pages describe the individual contracts.

## Delivery

A game mode mounts the addon’s s&box package if, and only if, the addon has an **S&box Identifier**. The identity and optional S&box Version are mounted as `organization.package#version` at runtime. An addon without an identifier only contributes content mappings and config, for example content already shipped in the DXRP game.

## Test the draft end to end

Create a test game mode or use a private one:

1. Add the addon.
2. Select the draft revision.
3. Apply the suggestions you want to test.
4. Select any further content entries needed for the test.
5. Apply addon-wide and content-level config overrides.
6. Configure jobs, market items, entities, or fixtures that consume the content.
7. Save the game mode and sync a test server.
8. Wait for runtime package mounting during startup/game-mode updates.

Check all of the following:

- the server can obtain the package;
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
- Are there new suggestions worth applying?
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

Only the newest draft is editable; historical published revisions remain available for game modes that still pin them. Keep the old revision available until operators have migrated and verified production.

## Release checklist

- [ ] The addon identifier and s&box package identity are correct.
- [ ] The addon has an S&box Identifier, and the version pin (if any) exists.
- [ ] No secrets, tokens, local saves, caches, or unrelated files are included.
- [ ] The revision was imported from the current `.dxaddon` definition.
- [ ] Every content reference matches the packaged path exactly.
- [ ] Content entry IDs and permission IDs were preserved where intended.
- [ ] Base config is valid JSON with useful code defaults.
- [ ] The changelog calls out operator action and restart/sync requirements.
- [ ] The draft was tested through a game mode on a clean server/client.
- [ ] An upgrade from the previous revision was tested.
- [ ] The game mode was checked after selecting the new revision.

For visibility, pricing, and entitlement behavior, continue to [Pricing & Licensing](/docs/developers/publishing/monetization).
