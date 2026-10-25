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
