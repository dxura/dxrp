---
title: Backups
description: Create, download, annotate, favorite, restore, and delete network data backups.
order: 2
---

**Where:** Open the account menu in the top-right, then **Backup**.

Backups cover network player balances, levels, inventories, and [Store](/docs/operators/administration/store) data. They do not include a server's saved world; use [server snapshots](/docs/operators/operations/servers#snapshots) for that.

## Automatic backups

An active network receives an automatic backup every hour. A server must have checked in within the last 90 minutes for the network to count as active.

## Create and label a backup

Use **Create Backup** before a risky bulk inventory, economy, or Store change. Manual backups are limited to one per hour.

Add a note that identifies the reason, such as `before rank migration`. You can favorite one backup to exempt it from normal retention; favoriting another replaces the previous favorite.

Normal backups are retained for seven days.

## Download or restore

Download a backup when you need an offline copy or want to inspect the JSON first.

**Restore Backup** replaces the network's current player balances, levels, inventories, and Store data with the selected backup. Online servers are notified so connected balances update without waiting for reconnect. Restores are limited to five per hour.

Restoring is a full replacement, not a merge. Download and inspect uncertain backups before restoring.

Deleting a backup is permanent. Backup access requires `backups.manage`.
