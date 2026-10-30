---
title: Store
description: Inspect and repair the persistent JSON key-value data shared by servers and addons.
order: 3
---

**Where:** Open the account menu in the top-right, then **Store**.

The Store is the selected network's persistent key-value data. Servers and addons use it for structured state that must survive restarts and be shared across the network.

## Browse and search

Entries are grouped by colon-separated key prefixes, for example:

```
myaddon:player:76561198000000000:progress
```

Search by key or value, expand a group to inspect its children, and open an entry to view its full JSON.

## Add or edit an entry

Select **New Entry** or edit an existing row. Keys are normalised to lowercase by the backend. Values must be valid JSON: use a quoted JSON string for plain text, or an object, array, number, boolean, or `null` as appropriate.

Do not change a value unless you understand the schema expected by the system that owns its prefix. A syntactically valid edit can still break an addon.

## Delete data

- **Delete Entry** removes one key.
- **Delete Group** removes the group entry.
- **Delete all children** removes every key below a prefix.

Take a [Backup](/docs/operators/administration/backups) before deleting a group or repairing unfamiliar production data.

## Limits and ownership

The Store allows keys up to 512 characters, values up to 1 MB, and 250,000 entries per network. It is not a bulk file store.

Addon authors should namespace their keys and document their value schema. [Persistence](/docs/developers/systems/persistence) covers the server-side API and failure behaviour.

Store access requires `store.manage`.
