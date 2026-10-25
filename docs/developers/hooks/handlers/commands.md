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

The built-in `WaypointCommand` (`game/Code/Chat/Commands/WaypointCommand.cs`) demonstrates command execution and [Store persistence](/docs/developers/systems/persistence).

For an end-to-end implementation, follow the [Command](/docs/developers/tutorials/command) tutorial.
