---
title: Factions
description: Review your network's factions, follow their treasuries and moderate them from the portal.
order: 4
---

**Where:** Open **More → Factions**.

Factions are player-run groups shared by every server on your network. Players found, join and run them in-game from the tab menu. The portal is where staff watch over them.

## Overview

The Factions page opens on network totals:

- **Factions** and **Members** across the network.
- **Held in treasuries**: money sitting in faction treasuries. It's outside every player's balance, so count it when you judge the size of your economy.
- **Net flow (7 days)**: credits minus debits across every treasury this week. A sudden spike is worth a look.
- **Inactive 30+ days**: factions where no member has been seen for a month.

Below that is the faction leaderboard, sortable by level, experience, members, treasury, last activity and founding date. Next to it, **Largest movements this week** lists the biggest single deposits and withdrawals, and **All Treasury Activity** is the network-wide ledger. Both link through to the faction involved.

Select a faction to open its page.

## Faction page

- **Header and stats**: tag, name, description, level, treasury, experience, members against the member cap, leader (links to the player), last activity and founding date.
- **Members**: role, leader badge and last seen. Players unseen for 30+ days are highlighted.
- **Roles**: each role's order (lower is more senior), permissions and member count.
- **Upgrades**: every upgrade the faction has bought, with its level.
- **Treasury Ledger**: every deposit, withdrawal, upgrade purchase and adjustment, filterable by type. Use it to trace money moving through a faction, for example when you suspect laundering or duplicated cash.
- **Audit Log**: renames, kicks, leadership changes, upgrade purchases and staff actions for this faction.

## Moderation

Staff with **Manage Factions** (`portal.faction.manage`) can act on a faction from its page. In-game rank rules don't apply to staff actions. Every action is written to the audit log and posted to your mod-log Discord webhook, and servers that are online reload the faction straight away.

| Action | Where | Notes |
| --- | --- | --- |
| Edit name, tag or description | **Edit** in the header | Tags stay unique across the network. |
| Disband | **Disband** in the header | Releases every member and removes the treasury. Can't be undone. The ledger is kept and stays visible in **All Treasury Activity**. |
| Change a member's role | Role dropdown on the member | |
| Make a member leader | Crown button on the member | The new and old leader swap roles. |
| Remove a member | Remove button on the member | The leader can't be removed; make someone else leader first. |
| Adjust the treasury | Click the treasury amount | A reason is required. The change appears in the ledger as an Adjustment. |
| Grant experience | Click the experience amount | The level updates when a server next loads the faction, using that server's level curve. |
| Set an upgrade level | **Set** on Upgrades, or the edit button on an upgrade | No treasury charge. |

### Cleaning up inactive factions

**Clean up** on the overview finds factions where no member has been seen for a number of days. **Preview** lists them with their member count and treasury, then **Disband** removes them all at once with an optional reason. Nothing is removed until you confirm, and only the factions in the preview are disbanded. Any that became active since are skipped.

## Permissions

- **View Factions** (`portal.faction.view`): the overview, faction pages and ledgers.
- **Manage Factions** (`portal.faction.manage`): everything under Moderation.
- **View Audit** (`portal.audit.view`) is also needed to see a faction's audit log.

## Configuring factions

Faction rules are part of your game mode's configuration under **Systems → Faction**:

- creation cost and base member slots
- invite lifetime
- whether the treasury is enabled, and the minimum and maximum transaction
- the level curve and maximum level
- the upgrades members can buy

See [Game modes](/docs/operators/operations/game-modes) for editing configuration.
