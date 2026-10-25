---
title: Lifecycle
description: Create a persistent addon service and react to player, map, and game-mode lifecycle events.
order: 1
---

`[AddonService]` gives your addon one persistent component per scene. That service, or any other component your addon adds to the scene, can implement `IGameEvents` to react to gameplay lifecycle changes. Discovery is automatic; there is no central registration list.

## Choose a service, an event hook, or both

| Need | Use |
| --- | --- |
| One long-lived component per scene | `[AddonService]` |
| Player, map, or game-mode lifecycle | `IGameEvents` |
| Server configuration application | [Configuration Events](/docs/developers/hooks/events/configuration) |
| Equipment deploy/drop/pickup lifecycle | [Equipment Events](/docs/developers/hooks/events/equipment) |
| A discoverable chat command | [Commands](/docs/developers/hooks/handlers/commands) |
| A typed portal-to-server action | [Server Actions](/docs/developers/hooks/handlers/server-actions) |

Most non-trivial addons use both: an `[AddonService]` component holds state and implements the event interfaces it needs. Content components do not need `[AddonService]`; their prefabs already control their lifecycle.

## `IGameEvents`

All methods have empty default bodies, so implement only the events you use:

```csharp
public interface IGameEvents : ISceneEvent<IGameEvents>
{
    void OnGameStartupCompleted() {}
    void OnPlayerJoined( Player player ) {}
    void OnPlayerSpawnedHost( Player player ) {}
    void OnPlayerKillHost( Player player ) {}
    void OnPlayerLifeStateChanged( Player player, LifeState before, LifeState after ) {}
    void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after ) {}
    void OnPlayerDisconnectHost( long steamId ) {}
    void OnMapFitted() {}
    void OnWeaponShot() {}
    void OnGameModeUpdated( GameModeDto? before, GameModeDto? after ) {}
    void OnAddonPackageMounted( GameModeAddonDto addon ) {}
    void OnSecondlyUpdate() {}
}
```

Methods ending in `Host` only fire on the host.

## Where each hook is delivered

An `ISceneEvent` can be posted to the whole scene (`Post`) or only to one GameObject and its descendants (`PostToGameObject`). A hook posted to a GameObject never reaches an `[AddonService]` component, and nothing warns you: the method simply never runs.

| Hook | Runs on | Delivered to | Notes |
| --- | --- | --- | --- |
| `OnGameStartupCompleted` | Every machine | Whole scene | Once, after config, addon mounting, networking, and core systems |
| `OnPlayerJoined` | Host | Whole scene | After the player object is network-spawned and active sanctions are applied |
| `OnPlayerSpawnedHost` | Host | Whole scene | Every (re)spawn |
| `OnPlayerKillHost` | Host | Whole scene | |
| `OnPlayerLifeStateChanged` | Every machine | Whole scene | When the synced `HealthComponent.State` changes |
| `OnPlayerJobChangedHost` | Host | Whole scene | |
| `OnPlayerDisconnectHost` | Host | Whole scene | After the reconnect grace period |
| `OnMapFitted` | Host | Whole scene | After the map is loaded and fitted; also posted when no fitting exists, so do not assume markers were placed |
| `OnWeaponShot` | Shooter's machine | **The weapon's GameObject only** | Not received by an addon service |
| `OnGameModeUpdated` | Every machine | Whole scene | See below |
| `OnAddonPackageMounted` | Every machine | Whole scene | Once per mounted s&box package |
| `OnSecondlyUpdate` | Every machine | Whole scene | Once a second after startup |

Equipment hooks are mostly GameObject-scoped; see [Equipment Events](/docs/developers/hooks/events/equipment). Damage hooks (`IDamageEvents`) are posted only to the victim's and the attacker's GameObjects, so they also need a component on the player rather than an addon service.

## When your service starts

An addon added to a running game mode mounts after `OnGameModeUpdated` and `OnGameStartupCompleted` have already been posted, so it never receives them. Do your initial setup in `OnStart` from the current state, and use the hooks only for changes that happen afterwards. [Addon Load Order](/docs/developers/hooks/events/load-order) covers the full startup sequence and which registries pick up a late-mounted package.

## Reacting to `OnGameModeUpdated`

It fires at startup with `before = null`, every time an operator saves the game mode, and on clients when the host's synced game mode arrives. Write the handler as a reconciler: rebuild your cached state from `after` (or `Config.Current.GameMode`) instead of applying a diff, so running it twice is harmless.

Changes you make to `Config.Current.GameMode` (for example `gameMode.Jobs.Add(...)`) stay on the machine that made them. The game mode is synced from the host, so the next update replaces them.

Core also handles this event: on the host, players whose job no longer exists are moved back to a job with the same name or to the default job. Handler order between core and addons is not guaranteed.

For a complete persistent-service example, follow the [Service](/docs/developers/tutorials/service) tutorial.
