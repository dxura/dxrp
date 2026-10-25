---
title: Equipment
description: Create an input-driven equipment item, compose its world prefab, and add it to a job or market.
order: 3
---

This tutorial creates a simple noise-maker equipment item. Pressing primary attack broadcasts a sound, which demonstrates the equipment input lifecycle without requiring a full weapon.

## 1. Write the behaviour component

Create `NoiseMakerEquipment.cs`:

```csharp
namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon;

public sealed class NoiseMakerEquipment : InputWeaponComponent, IInputHints
{
    [Property] [Group( "Effects" )]
    public required SoundEvent UseSound { get; set; }

    [Property] [Range( 0.1f, 10f )]
    public float UseCooldown { get; set; } = 1f;

    IEnumerable<(string Action, string Label)> IInputHints.GetInputHints()
    {
        yield return ("attack1", "Make noise");
    }

    protected override void OnInputDown()
    {
        if ( !Player.Local.IsValid()
            || Cooldown.Current.CheckAndStartCooldown(
                "my-addon:noise-maker",
                UseCooldown,
                true ) )
        {
            return;
        }

        UseSound.Broadcast(
            Player.Local.WorldPosition,
            Player.Local.GameObject );
    }
}
```

`InputWeaponComponent` processes input only for the local owner while this equipment is deployed. Its default input action is `Attack1`.

## 2. Create the world prefab

Create:

```
equipment/noise-maker/w_noise-maker.prefab
```

Add these components to the world-prefab root:

- `Equipment`;
- `TagBinder` (required by `Equipment`);
- `NoiseMakerEquipment`; and
- a `SkinnedModelRenderer` assigned to the `Equipment.ModelRenderer` property.

Assign the sound event on `NoiseMakerEquipment`. Configure `Equipment` fields such as hold type, handedness, dropped size, crosshair behaviour, and whether the item is purchasable.

The behaviour component should be a sibling of `Equipment`; `WeaponComponent` finds the equipment on the same game object.

## 3. Decide whether you need a view model

A view model is optional. Without one, the item still works and uses its world model for third-person/drop rendering.

If you add one, create `equipment/noise-maker/vm_noise-maker.prefab` with a `ViewModel` component and first-person model/animation setup. You can assign it on `Equipment.ViewModelPrefab` or use the content entry's secondary reference.

## 4. Create the content entry

Add content to the draft revision:

| Field | Value |
| --- | --- |
| Type | `Equipment` |
| Name | `Noise Maker` |
| Primary Reference | `equipment/noise-maker/w_noise-maker.prefab` |
| Secondary Reference | `equipment/noise-maker/vm_noise-maker.prefab` (optional) |

Both references are relative to the asset-package root. Save the draft, attach it to a test game mode, and include the equipment content.

On the game-mode addon mapping, choose **Package** when the addon uses a published s&box package or **Launcher** when the revision contains DXRP Code/Assets bundles. Loading the addon and adding the equipment to a job or market are separate steps.

## 5. Put it in gameplay

An equipment entry does nothing until the game mode uses it. Add it to:

- a job's equipment loadout;
- a market item; or
- another system that grants equipment content.

Test deploy, holster, drop, pickup, death, and job-change behaviour. Confirm another client hears the broadcast and that the input does not run while the item is holstered.

For weapons, compose `AmmoComponent`, `ShootWeaponComponent`, recoil, reload, aim, melee, or projectile components instead of placing all behaviour in one class. Continue to [Content Entries](/docs/developers/publishing/content-entries) for the portal mapping.
