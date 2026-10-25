---
title: Ranks
description: Define staff and player ranks, inheritance, server scope, and portal and in-game permissions.
order: 3
---

**Where:** Open **More → Ranks**.

Ranks control both portal access and in-game authority. Build them from least to most trusted, then assign them from a player's [profile](/docs/operators/community/players).

## Rank fields

A rank can define:

- its name, colour, and order;
- whether it is the default rank;
- a parent rank to inherit from;
- the servers where it applies; and
- allowed or denied permissions.

Lower order values are evaluated before higher ones in places where ordering matters. Only one rank should be the network default.

## Inheritance

Choose **Inherits From** to reuse a parent rank's permissions. The portal prevents direct inheritance cycles, but deep trees are still hard to reason about. Prefer a small hierarchy such as:

```
Player
└── Moderator
    └── Administrator
```

An explicit deny can override an inherited allowance. Review both the rank's own permissions and the inherited list before troubleshooting access.

## Server scope

Use **All Servers** for network-wide roles. Limit a rank to selected servers when a staff member or whitelist group should apply only there.

## Permissions

Grant the smallest set needed for the role. Portal permissions cover areas such as servers, players, addons, game modes, sanctions, audit, Store, backups, and API keys. Addon-defined permissions appear alongside built-in permissions after the addon is available to the network.

Avoid **All Permissions (Wildcard)** for routine staff roles. It automatically includes new permissions added later.

## Assigning ranks

Open **Players**, select a profile, and update its ranks. You cannot assign a rank above your own authority or change your own rank. Currently the first assigned rank is the one active in-game, so order assignments deliberately.

Rank access uses `ranks.view`, `ranks.edit`, and `ranks.assign`.
