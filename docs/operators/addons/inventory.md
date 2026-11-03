---
title: Inventory
description: Create item definitions, manage images, inspect ownership, and perform bulk grants or revocations.
order: 2
---

**Where:** Open **More → Inventory**.

Inventory separates item definitions from player ownership. Editing or deleting a definition affects the shared item type; adding or removing it from one player changes only that player's inventory.

## Item definitions

Select **Create New Item** and configure the name, description, type, rarity, stack behaviour, trading/market options, and what the item grants. Copy the item ID when another system needs to reference it.

### What an item grants

| Item type | Grants |
| --- | --- |
| Emote | An **Emote** content |
| Vehicle | A **Vehicle** content |
| Consumable | An **Entity** content, spawned when the item is used |
| Anything else | An optional plain identifier, e.g. a title's text |

For content grants, open the item, select **Edit**, pick a game mode to browse, then the content. The item keeps the content's own id, not the game mode's, so it grants that content in every game mode that includes it. If a game mode doesn't include the content, the item does nothing there.

Images must be PNG, JPEG, or WebP and no larger than 5 MB. You can download, replace, or clear the current image.

## Ownership

Open an item to inspect its owners. For one player, it is usually faster and safer to manage inventory from their [Players](/docs/operators/community/players) profile.

Deleting a definition is different from revoking it. Confirm there are no remaining owners or game-mode references before deleting the definition.

## Bulk inventory

Bulk operations grant or revoke selected items across a player set. Use the preview before applying:

1. choose the target players or ranks;
2. select one or more item definitions;
3. choose grant or revoke and the quantity behaviour;
4. review the preview; and
5. apply once the affected count is correct.

Leave revoke quantity empty only when you intend to remove all quantities.

Inventory access uses `items.view` and `items.manage`.
