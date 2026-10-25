---
title: Addon Load Order
description: When addon code exists relative to core startup, configuration, and players joining, and how to initialize safely.
order: 0
---

[Lifecycle](/docs/developers/hooks/events/lifecycle) covers gameplay events: a player spawns, a job changes, the game mode is saved. This page covers the question that comes first: **when does your addon's code exist at all?**

An addon loads late. Core singletons have already started, the configuration has already been fetched, and on a client the player has usually already joined. Code that assumes otherwise works in the editor and fails on a live server.

## Core startup sequence

Every machine (host and clients) runs the same sequence:

| Step | What happens | Notes |
| --- | --- | --- |
| 1 | Configuration loads | The host fetches it from the API; a client waits for the host's synced copy |
| 2 | The game mode's addon packages mount | `OnAddonPackageMounted` is posted once per package |
| 3 | Networking initializes | |
| 4 | Core systems are enabled | Registries start here and create the `[AddonService]` components |
| 5 | Initial events are posted | `OnConfigOverride` (if any), `OnGameModeUpdated( null, gameMode )`, `OnConfigAppliedHost` (host), then `OnGameStartupCompleted` |
| 6 | Players join | The host posts `OnPlayerJoined` for each one |

Packages mounted in step 2 are visible to every registry that starts in step 4, so an addon that is part of the game mode when the server starts sees everything.

## An addon added to a running game mode

When an operator attaches an addon to a game mode and saves it, the running server:

1. receives the new game mode and posts `OnGameModeUpdated`;
2. starts mounting the new package in the background; and
3. posts `OnAddonPackageMounted` once it has mounted.

Your code does not exist during step 1, so your service never receives that `OnGameModeUpdated`. It is created during step 3, after `OnGameStartupCompleted` has already been posted. Players are already in the world.

Do your initial setup in `OnStart`, from the current state:

```csharp
[AddonService]
public sealed class CityJobsService : Component, IGameEvents
{
    protected override void OnStart()
    {
        // Works whether the addon loaded at startup or was mounted later.
        Rebuild( Config.Current.GameMode );

        if ( Networking.IsHost )
        {
            foreach ( var player in GameUtils.Players )
            {
                SetUpPlayer( player );
            }
        }
    }

    void IGameEvents.OnGameModeUpdated( GameModeDto? before, GameModeDto? after ) => Rebuild( after );

    void IGameEvents.OnPlayerSpawnedHost( Player player ) => SetUpPlayer( player );
}
```

Write `Rebuild` and `SetUpPlayer` so that running them twice is harmless: at startup the service runs `OnStart` and then receives the initial `OnGameModeUpdated`.

## What picks up a late-mounted package

Not every core registry rescans when a package mounts. Until the server restarts, a package mounted after startup gets:

| Discovered by type | Picked up after a late mount |
| --- | --- |
| `[AddonService]` components | Yes |
| `System` content entries | Yes |
| Statuses (`IStatus` / `BaseStatus`) | Yes |
| Fixture placements | Yes, the host re-spawns them |
| Tab menu sections (`ITabMenuSectionProvider`) | Yes, rebuilt each time the menu opens |
| Chat commands (`ICommand`) | **No**, registered once when core systems start |
| Construct definitions (`IConstructDefinition`) | **No** |
| Player actions (`IPlayerActionProvider`) | **No**, built once on first use |
| Server action handlers (`IActionHandler`) | **No**, registered once at startup |

If your addon ships commands or other types from the "No" rows, tell operators in the changelog to restart the server after attaching or updating it.

## Clients

A client goes through the same sequence when it connects. By the time its own services start, the host has been running for a while: entities exist, statuses are already applied, and one-off broadcast RPCs sent before the client connected are gone. Build client-side state from synced properties (`[Sync]`), not from the history of broadcasts you expect to have seen.

## Host or client?

Decide inside the event that needs it, not once in `OnStart`. Prefer the `Host`-suffixed events for host-only work: they only ever run on the host. When you do need a check in `OnStart`, use `Networking.IsHost`, and keep in mind that a service on a client may start while the connection is still being established.

## Checklist

- Initial setup reads the current state in `OnStart`; events only handle later changes.
- Every handler can run twice without side effects.
- Nothing assumes `OnGameStartupCompleted` or the first `OnGameModeUpdated` reached your code.
- Players already in the world are handled, not just the ones who join later.
- Client visuals rebuild from synced state for late joiners.
- Test by attaching the addon to a game mode **while the server is running**, then by joining with a second client.
