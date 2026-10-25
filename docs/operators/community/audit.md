---
title: Audit
description: Search the network audit trail by player, entity, and action.
order: 2
---

**Where:** Open **Audit**.

Audit records explain who or what changed network state. Use them to investigate a report, verify an operator action, or understand an unexpected entity or economy change.

## Filter the log

You can narrow the table by:

- player ID;
- entity ID;
- included action types; and
- excluded action types.

Use inclusion filters when you know the event you need. Exclusions are useful for hiding noisy routine events. The portal limits the number of simultaneous action filters, so start broad and narrow after you see the available actions.

Select a player ID in a result to open their [Players](/docs/operators/community/players) profile.

## Refreshing

Use manual refresh for a one-off investigation or enable auto-refresh while watching a live incident. Broad queries can time out on a busy network; add a player, entity, or action filter and try again.

## Audit versus sanctions

Audit is the general event history. [Sanctions](/docs/operators/community/sanctions) is the structured moderation record for warnings, gags, kicks, jails, and bans. A sanction can appear in both places for different reasons: Sanctions holds its current state, while Audit records actions taken around it.

Viewing the audit log requires `audit.view`.
