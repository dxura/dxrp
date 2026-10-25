---
title: Network
description: Configure network identity, integrations, API keys, billing, and member access.
order: 1
---

**Where:** Open the account menu in the top-right, then **Network**.

The page contains the tabs your rank can access: **Network**, **API Keys**, and **Billing**. Settings apply to the selected network, not to one server.

## Network settings

Configure:

- display name and description;
- network identifier;
- square marketplace/portal thumbnail;
- Discord invite URL;
- moderation log webhook;
- media webhook; and
- general log webhook.

Webhook fields accept Discord webhook URLs only. Keep each event type in the staff channel that needs it, and leave a field blank to disable that route.

Editing these fields requires `network.settings.edit`.

## Invite a member

The account menu also exposes **Invite** when your rank can assign ranks. Enter the member's Steam64 ID and choose a rank with portal access. Review the rank first: an invitation immediately defines what that member can see and change.

## API Keys

Create API keys for bots and server-side integrations. Give each key a description, optional expiry, and only the permissions it needs. The key value is shown once.

Use **Revoke** for one integration or **Revoke All** after a broader leak. Never put a key in browser code, screenshots, chat, or a public repository.

The [API Documentation](/docs/api/api-documentation) covers headers, scopes, rate limits, and examples. Key management requires `apikeys.manage`.

## Billing

Billing configures the payment recipient used by monetization and shows the fees applied to store, subscription, and addon sales. Confirm the Steam64 recipient before saving; changing it affects future payouts rather than rewriting completed transactions.

See [Monetization](/docs/operators/monetization/monetization) for products, coupons, sales, and earnings.
