---
title: Equipment Events
description: React to equipment deployment, holstering, dropping, pickup, and destruction.
order: 3
---

Implement `IEquipmentEvents` when an addon needs to observe equipment lifecycle changes:

```csharp
public interface IEquipmentEvents : ISceneEvent<IEquipmentEvents>
{
    void OnEquipmentDropped( DroppedEquipment dropped, Player? player ) {}
    void OnEquipmentPickedUp( Player player, DroppedEquipment dropped, Equipment equipment ) {}
    void OnEquipmentDeployed( Equipment equipment ) {}
    void OnEquipmentHolstered( Equipment equipment ) {}
    void OnEquipmentDestroyed( Equipment equipment ) {}
}
```

Implement only the methods the addon uses. Host-authoritative behavior must still validate that it is running on the host before changing shared game state.

## Who receives each event

Only drop and pickup are posted to the whole scene. Deploy, holster, and destroy are posted to the equipment's root GameObject, which is the holding player's GameObject while the item is held:

| Event | Runs on | Delivered to |
| --- | --- | --- |
| `OnEquipmentDropped` | Host | Whole scene |
| `OnEquipmentPickedUp` | Host | Whole scene |
| `OnEquipmentDeployed` | Every machine | Equipment root and its descendants only |
| `OnEquipmentHolstered` | Every machine | Equipment root and its descendants only |
| `OnEquipmentDestroyed` | Every machine | Equipment root and its descendants only |

An `[AddonService]` component lives on the core systems object, not under a player, so it **never** receives deploy, holster, or destroy. There is no error or warning; the method just never runs. Implement those three on a component of the equipment prefab itself (as core's `MedKitEquipment` and `HandsEquipment` do), or on a component you attach to the player.

Deploy and holster follow the synced `IsDeployed` state, so they run on every machine, and one of them also runs when the equipment starts. A player who joins late therefore receives `OnEquipmentDeployed` or `OnEquipmentHolstered` for equipment that already exists.

## Input hints

Equipment that needs contextual controls can implement `IInputHints`:

```csharp
public interface IInputHints
{
    IEnumerable<(string Action, string Label)> GetInputHints();
    int GetInputHintsHash() => 0;
}
```

Return the input action and its player-facing label, such as `("reload", "Reload")`. Override the hash when the available hints change dynamically so the UI knows to refresh them.

Follow the [Equipment](/docs/developers/tutorials/equipment) tutorial for a complete input-driven equipment example.
