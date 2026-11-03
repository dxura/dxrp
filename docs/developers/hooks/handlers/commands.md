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
    CommandForm[] Usage => Array.Empty<CommandForm>(); // argument syntax, one entry per form
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

    // Command-specific values, on top of the ICommandSuggestionProvider for the type
    IEnumerable<CommandSuggestion> GetSuggestions( CommandParam param, IReadOnlyList<string> args ) => [];
}
```

`ExecuteHost` runs the command on the server. `ExecuteLocal` is an opt-in for commands that only drive local client UI; return `true` to stop the command from going to the host.

## Usage and suggestions

Typing `/` in chat opens a command helper. It lists the commands the player can use, then shows the typed command's usage with the current parameter highlighted, and suggests values for it. Players move through suggestions with the arrow keys and pick one with Enter (Tab works too).

`Usage` describes the arguments with a compact syntax, one entry per form the command accepts:

| Syntax | Meaning |
| --- | --- |
| `list` | A literal word |
| `set\|add` | One of several literal words |
| `<player>` | Required argument |
| `[amount]` | Optional argument |
| `<reason...>` | Takes the rest of the line |
| `<on\|off>` | Argument with fixed choices |
| `<target:player>` | Argument named `target` that takes `player` values |

```csharp
public CommandForm[] Usage => ["list", "add|remove <player> <amount>", "reset [player]"];
```

### Gating subcommands

Plain strings convert to ungated forms. To restrict a form, construct a `CommandForm` with permissions, addon permission IDs, or a condition:

```csharp
public CommandForm[] Usage =>
[
    "list",
    new( "add|remove <player> <amount>", Permission.CommandMyThingManage ),
    new( "reset [player]" ) { PermissionIds = ["my-addon.reset"] },
    new( "approve|deny" ) { Condition = p => p.Job.IsChiefRole() }
];
```

A gated form is hidden from players who can't use it, and the command disappears from the list when none of its forms are usable. On the host, the same forms are the permission check: input aimed only at forms the player can't use is rejected, with the missing permissions named, before `ExecuteHost` runs. This applies to both typed commands and `ExecuteCommandHost`, so don't repeat the check in `ExecuteHost`.

The host works out which forms the input is aimed at like this:

1. Forms the arguments fit exactly: literals match, required positions are filled, and there are no extra words unless the last position takes the rest.
2. If none fit exactly, forms whose leading subcommand words match, however many words follow. `/minigame join extra` still counts as `join`.
3. Forms with literal words beat purely positional ones. `/waypoint set home` targets `set|clear <name...>`, not `<name...>`.

Input that fits no form at all reaches `ExecuteHost` ungated, so the command can report its own usage error. For the gate to hold, every branch that does something must have a form: declare a bare `""` form if the command acts with no arguments, and list hidden aliases among a literal's choices (`show|info|get`). `Condition` runs on both the client and the host, so it should only read state both sides have, such as the player's job.

### Suggestions

A parameter gets its suggestions from every `ICommandSuggestionProvider` whose `Type` matches the parameter's type. That's the part after the colon, or the name itself: `<player>` and `<target:player>` both use `player`. Providers are discovered through `TypeLibrary` like commands, so an addon adds a value kind by implementing one:

```csharp
public sealed class CrateSuggestionProvider : ICommandSuggestionProvider
{
    public string Type => "crate";

    public IEnumerable<CommandSuggestion> GetSuggestions( CommandParam param, IReadOnlyList<string> args ) =>
        MyCrates.All.Select( c => new CommandSuggestion( c.Id, c.Name, c.Description ) );
}

// Any command can now write "<crate>" or "<from:crate>"
```

Built-in types: `player` (online players; a name with a space inserts the SteamID), `playername` (the same, but a name with a space inserts it quoted, for commands that parse quotes themselves), `duration`, `job`, `status`, `minigame`, `equipment` and `event`. Providers that share a type all contribute. Providers run on the client, and the helper filters their results against what the player has typed.

For values only one command needs, override `GetSuggestions` on the command instead. It runs before the providers for the parameter's type, and its results are added to theirs.

The helper only reads `Usage`, so parsing and validating `args` in `ExecuteHost` is still up to the command.

`RequiredPermissionIds` gates a command behind permissions declared by your addon. See [Permissions](/docs/developers/systems/permissions) for declaration and operator assignment.

## Replying to the player

Command feedback goes in the player's chat, next to the command they typed, not in a notification. Use the chat helpers on `Player`, which accept plain text or a `#phrase` key:

| Helper | Use for |
| --- | --- |
| `caller.SendMessage( text )` | Neutral output, such as information the player asked for |
| `caller.ChatSuccess( text )` | The command did what was asked |
| `caller.ChatError( text )` | The command failed or was refused |
| `caller.ChatWarn( text )` | Something needs attention, such as a usage problem |
| `caller.ChatCooldown( cooldownId )` | The command is cooling down; shows the seconds left |

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
