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
    void OnPlayerJoined( Player player ) {}
    void OnPlayerSpawnedHost( Player player ) {}
    void OnPlayerKillHost( Player player ) {}
    void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after ) {}
    void OnPlayerDisconnectHost( long steamId ) {}
    void OnMapFitted() {}
    void OnWeaponShot() {}
    void OnGameModeUpdated( GameModeDto? before, GameModeDto? after ) {}
    void OnSecondlyUpdate() {}
}
```

Methods ending in `Host` only fire on the host. `OnGameModeUpdated` is the hook to use when your addon must react to changes in its content or game-mode configuration.

For a complete persistent-service example, follow the [Service](/docs/developers/tutorials/service) tutorial.
