---
title: Permissions
description: Declare a custom permission for your addon, check it in code, and understand who it's granted to.
order: 3
---

## Declaring a custom permission

An addon revision can declare its own permissions alongside its content entries: each one an `Identifier`, `Name`, `Description`, and `Category`. The portal automatically prefixes your identifier with your addon's own identifier (e.g. `custom_kill` becomes `my-addon.custom_kill`), so you don't need to worry about colliding with another addon's permission names.

Choose identifiers for capabilities, not UI controls. `my-addon.manage-printers` remains meaningful if the button later becomes a chat command; `my-addon.blue-button` does not.

Published identifiers should be treated as permanent. Renaming one creates a different permission and leaves existing rank grants pointing at the old ID.

## Checking it in code

Gate a chat command behind it with `RequiredPermissionIds` (see [Commands](/docs/developers/hooks/handlers/commands)):

```csharp
public string[] RequiredPermissionIds => new[] { "my-addon.custom_kill" };
```

For an interaction or RPC, check the full ID directly:

```csharp
if ( !RankSystem.HasPermission( caller.SteamId, "my-addon.custom_kill" ) )
{
    caller.Error( "You do not have permission to do that." );
    return;
}
```

There's no separate addon permission store. Built-in enum permissions and addon string IDs are resolved against the same effective ranks.

Permission checks that protect shared state must run on the host. A client-side check is useful for hiding a button, but it is not authorization; validate again inside the host command/RPC before changing anything.

## Permissions are granted to ranks, not jobs

This is the part that trips people up: a game mode's **jobs** have no permission field at all. Permissions, both built-in and yours, are assigned to **ranks** from the portal's rank manager. A player gets your addon's permission by having a rank that grants it, independent of whatever job they currently hold. When an operator opens the rank manager, your addon's declared permissions show up grouped by category alongside the built-in ones, visually separated so it's clear which addon they came from.

Practically, document which of your addon's features need which permission identifier, and point operators at their rank manager to grant it. There's no job-level equivalent to direct them to instead.

## Design checklist

- Default to denying privileged actions.
- Keep read and write capabilities separate when staff may need one without the other.
- Validate on the host using the caller's Steam ID.
- Document every permission in the addon description or changelog.
- Never repurpose an existing identifier for a different capability.

Continue to [Persistence](/docs/developers/systems/persistence) when the protected feature also needs to save state.
