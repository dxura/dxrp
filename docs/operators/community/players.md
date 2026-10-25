---
title: Players
description: Find player profiles and manage notes, economy, level, ranks, inventory, and moderation.
order: 1
---

**Where:** Open **Players**, then select a player.

Search by name or Steam64 ID. You can filter by rank or online status and hide system/AI players when you only want community members.

## Player profile

The profile brings the operator actions for one player together:

- account and network identity;
- staff notes;
- balance and level;
- assigned ranks;
- inventory;
- sanctions; and
- audit history.

Use **Audit Log** to investigate what the player did before changing their data or issuing a sanction.

## Notes

Staff notes are internal context for future operators. Record facts and decisions, not passwords, tokens, or sensitive personal information.

## Balance and level

Balance and level changes apply to the player's network account. Because these are direct administrative edits, leave an explanatory note or confirm the corresponding entry in [Audit](/docs/operators/community/audit).

## Ranks

Assign one or more ranks from the profile. You cannot edit your own rank or assign a rank above your authority. Currently only the first assigned rank is active in-game, so put the intended primary rank first.

Configure rank inheritance and permissions under [Ranks](/docs/operators/community/ranks).

## Inventory

Open the inventory dialog to add or remove an item definition created under [Inventory](/docs/operators/addons/inventory). Removing a quantity from a stack does not delete the item definition itself.

## Sanctions and alternate accounts

Issue a warning, note, gag, kick, jail, or ban from the player profile. Timed jails and gags need a duration; leaving a ban expiry empty makes it permanent. Existing sanctions can be pardoned, expired, and marked with appeal outcomes.

The **Alt Inspector** compares the signals available to your rank, such as IP or hardware matches. Treat a match as an investigation lead, not proof by itself.

See [Sanctions](/docs/operators/community/sanctions) for the network-wide queue.

## Permissions

Player data is split across permissions so staff can be given only what they need: `players.view`, `player.view`, `player.notes.manage`, `player.alts.view`, `ranks.assign`, and the relevant economy, inventory, or sanction permissions.
