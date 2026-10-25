---
title: Rulesets
description: Write and maintain the player-facing rules assigned to servers.
order: 4
---

**Where:** Open **More → Rulesets**.

A ruleset is a named rules document that can be assigned to one or more servers. It is not a gameplay configuration; gameplay behaviour belongs in [Game Modes](/docs/operators/operations/game-modes) and the server configuration override.

## Create a ruleset

Select **Create New Ruleset**, enter a clear name, and write the rules in the description editor. The editor supports Markdown and includes a preview.

Keep rules:

- specific enough to enforce consistently;
- grouped under descriptive headings;
- explicit about network-wide versus server-specific behaviour; and
- updated when staff policy or gameplay changes.

## Assign it

Open [Servers](/docs/operators/operations/servers), edit a server, and choose the ruleset. Reusing one ruleset across servers keeps shared policy consistent; create separate rulesets only where the rules genuinely differ.

Deleting a ruleset can leave servers without the intended document. Reassign affected servers before deleting it.

Ruleset access uses `rulesets.view` and `rulesets.edit`.
