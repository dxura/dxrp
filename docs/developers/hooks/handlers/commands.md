---
title: Commands
description: Define automatically discovered chat commands, execution behavior, cooldowns, and permissions.
order: 1
---

Implement `ICommand` to add a discoverable chat command. Commands are found automatically through `TypeLibrary`; there is no central registration list.

```csharp
public interface ICommand
{
    string Command { get; }               // used after '/', e.g. "pm"
    string[] Aliases => Array.Empty<string>();
    string Help { get; }
    bool IsUsableWhileDead => true;
    bool IsUsableWhileRestricted => false;
    bool IsUsableWhileFrozen => true;
    bool IsUsableWhileStunned => false;
    float? CooldownOverride => null;      // null = GameConfig.CommandCooldown
    Permission[] RequiredPermissions => Array.Empty<Permission>();
    string[] RequiredPermissionIds => Array.Empty<string>();

    bool ExecuteHost( Player caller, string[] args, string raw );
    bool ExecuteLocal( string[] args, string raw ) => false;
    void OnFrame() {}
}
```

`ExecuteHost` runs the command on the server. `ExecuteLocal` is an opt-in for commands that only drive local client UI; return `true` to stop the command from going to the host.

`RequiredPermissionIds` gates a command behind permissions declared by your addon. See [Permissions](/docs/developers/systems/permissions) for declaration and operator assignment.

## What runs before `ExecuteHost`

A command reaches `ExecuteHost` either typed in chat or through `Chat.Current.ExecuteCommandHost( name, args )` from code or UI. Both paths share the same host-side gate; the chat path adds the chat checks in front of it:

| Check | Typed in chat | `ExecuteCommandHost` |
| --- | --- | --- |
| Client chat cooldown (`ChatCooldown`) | Yes | No |
| `ExecuteLocal` | Yes, on the client, before anything is sent | No |
| Host chat cooldown per caller (`ChatCooldown`) | Yes | No |
| Gagged players rejected | Yes | No |
| Command cooldown (`CooldownOverride` or `CommandCooldown`) | Yes | Yes |
| `IsUsableWhileDead` / `IsUsableWhileRestricted` | Yes | Yes |
| Required permissions | Yes | Yes |

The command cooldown is shared by all commands of one player. Return `0f` from `CooldownOverride` for a command that must never be throttled.

Two consequences:

- A command that must still work for a gagged player cannot rely on the chat path. Call it with `ExecuteCommandHost` from your UI, or run it from `ExecuteLocal` and call your own host RPC.
- Code that leaves the pipeline, such as an `ExecuteLocal` that calls an RPC, skips every host-side check above. That RPC must do its own permission, state, and rate-limit checks on the host.

To rate-limit yourself, use `Cooldown.Current.CheckAndStartCooldown( id, seconds )`. It returns `true` while the cooldown is **still running** (meaning "blocked"), and starts it otherwise:

```csharp
if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:my-addon:action", 5f ) )
{
    return; // still cooling down
}
```

The built-in `WaypointCommand` (`game/Code/Chat/Commands/WaypointCommand.cs`) demonstrates command execution and [Store persistence](/docs/developers/systems/persistence).

For an end-to-end implementation, follow the [Command](/docs/developers/tutorials/command) tutorial.
