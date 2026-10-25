---
title: Sanctions
description: Review network moderation records, manage appeals, and pardon or expire active sanctions.
order: 5
---

**Where:** Open **More → Sanctions**.

This tab is the network-wide moderation queue. To issue a new sanction, open the target under [Players](/docs/operators/community/players); use this page to review and manage existing records.

## Sanction types

| Type | Effect |
| --- | --- |
| Warning | Records a warning without another automatic effect. |
| Note | Adds an internal moderation record. |
| Gag | Prevents chat use for the configured duration. |
| Kick | Disconnects the player. |
| Jail | Restricts the player in-game for the configured duration. |
| Ban | Prevents the player from connecting until expiry or pardon. |
| Automatic | Identifies a sanction issued by a system rather than a person. |

A sanction can include a reason, internal notes, duration, server scope, and modifiers such as clearing constructs, killing, or immediately kicking the player.

## Review the queue

Filter by player, type, or state. Open the linked player before acting when you need their notes, alternate-account signals, inventory, or [audit history](/docs/operators/community/audit).

## Pardon, expire, and appeals

- **Pardon** reverses the sanction while retaining its record.
- **Expire** ends an active timed sanction immediately.
- **Appealed**, **Accepted**, and **Denied** flags track the appeal workflow without rewriting the original action.

Record why a sanction was changed so another operator can reconstruct the decision.

## Discord routing

Set the moderation webhook under [Network](/docs/operators/administration/network) if sanction activity should also reach a Discord staff channel.

Viewing, issuing, and pardoning sanctions use separate permissions: `sanctions.view`, `sanctions.issue`, and `sanctions.pardon`.
