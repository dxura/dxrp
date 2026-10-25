---
title: Pricing & Licensing
description: How pricing, visibility, and licensing gate access to a published addon.
order: 4
---

## Pricing and visibility

Two independent fields on your addon control access:

- **`Visibility`**: `Private` or `Public`. Private addons are invisible to networks that don't already have access.
- **`Price`**: whole dollars, `0` meaning free.

There's also a per-addon `ShowSourceCode` flag (defaults on) that independently controls whether installers can see your source, separate from whether they can use the addon at all.

Access resolves like this for any network that isn't the addon's own author:

| | Source visible | Assets/code usable |
| --- | --- | --- |
| Free, `ShowSourceCode` on | Yes | Yes |
| Free, `ShowSourceCode` off | No | Yes |
| Paid/Private, entitled | Yes | Yes |
| Paid/Private, not entitled | No | No |

## Licensing a paid or private addon

A network becomes "entitled" to your addon one of two ways:

- **Self-claim**: only works for addons that are both `Public` and free (`Price == 0`). Any network can claim these directly from the marketplace; there's no approval step.
- **Explicit grant**: required for anything paid or private. As the author, you grant a specific network access from your addon's manage page (by their network identifier); this shows up for them under their portal's "Licensed Addons" list. Grants can optionally expire and are idempotent: granting the same network twice just reactivates the existing entitlement rather than duplicating it.

If you're building a paid or invite-only addon, plan for the manual-grant workflow rather than expecting self-service claiming to work: it's deliberately restricted to the free+public case.

Pricing comes after a stable release. See [Publishing Revisions](/docs/developers/publishing/publishing-addons) for drafts, compatibility checks, and the release checklist.
