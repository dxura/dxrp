---
title: Inventory
description: Create item definitions, manage images, inspect ownership, and perform bulk grants or revocations.
order: 2
---

**Where:** Open **More → Inventory**.

Inventory separates item definitions from player ownership. Editing or deleting a definition affects the shared item type; adding or removing it from one player changes only that player's inventory.

## Item definitions

Select **Create New Item** and configure the name, description, type, rarity, stack behaviour, trading/market options, and grant identifier required by the item. Copy the item ID when another system needs to reference it.

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
