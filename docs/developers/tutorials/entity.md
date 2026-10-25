---
title: Entity
description: Create a networked kickable ball, turn it into a prefab, and expose it as an entity content entry.
order: 2
---

This tutorial builds a physics entity that players kick by walking into it. It follows the same ownership pattern as DXRP's built-in sporting addon.

## 1. Write the component

Create `KickableBallEntity.cs` in your addon code folder:

```csharp
namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon;

[Title( "Kickable Ball" )]
[Category( "Entities" )]
public sealed class KickableBallEntity : BaseEntity, Component.ITriggerListener
{
    [Property] [Range( 100f, 1500f )]
    public float KickForce { get; set; } = 600f;

    [Property] [Range( 0f, 1f )]
    public float UpwardBias { get; set; } = 0.25f;

    [Property] [Range( 0.1f, 1f )]
    public float KickCooldown { get; set; } = 0.2f;

    [Property]
    public SoundEvent? KickSound { get; set; }

    private TimeSince _lastKick = 10f;

    void ITriggerListener.OnTriggerEnter( Collider other )
    {
        if ( Application.IsHeadless
            || _lastKick < KickCooldown
            || !other.Tags.Has( Constants.PlayerTag ) )
        {
            return;
        }

        var player = other.GameObject?.Root.GetComponent<Player>();
        if ( !player.IsValid() || player != Player.Local || !Rigidbody.IsValid() )
        {
            return;
        }

        if ( !Network.IsOwner && !GameObject.Network.TakeOwnership() )
        {
            return;
        }

        var direction = (WorldPosition - player.WorldPosition).Normal;
        direction = (direction + Vector3.Up * UpwardBias).Normal;

        Rigidbody.Velocity = direction * KickForce;
        _lastKick = 0f;

        if ( KickSound is not null )
        {
            KickSound.Broadcast( WorldPosition );
        }
    }
}
```

The local player that enters the trigger requests ownership before changing velocity. That owner simulates the rigid body, while the network distributes the result.

## 2. Build the prefab

Under your addon assets folder, create:

```
entities/kickable-ball/kickable-ball.prefab
```

On the networked root object, add:

- `KickableBallEntity`;
- `Rigidbody`;
- a normal collider for physics;
- a slightly larger trigger collider for player detection; and
- optional `HealthComponent`.

Assign the `Rigidbody` property on `KickableBallEntity`. Put the model renderer on the root or a child and assign the optional sound event.

Useful root tags are `entity`, `pocket_item`, `occludable`, and `hands_interact`, depending on how the ball should interact with core systems.

## 3. Create the content entry

In a draft revision, select **Add Content**:

| Field | Value |
| --- | --- |
| Type | `Entity` |
| Name | `Kickable Ball` |
| Primary Reference | `entities/kickable-ball/kickable-ball.prefab` |

Save the draft. The path must be relative to the asset root and match the packaged prefab exactly.

## 4. Add it to a game mode

Attach the draft addon to a test game mode, add the entity content, configure its health/lifecycle settings, save, and sync the test server.

Choose the load method that matches your revision: **Package** for a published s&box package, or **Launcher** for uploaded DXRP Code/Assets bundles. The entity content will not spawn until the game mode includes the entry even after the package is loaded.

Spawn the entity and verify:

- only the colliding local player attempts to take ownership;
- kicks are visible to another client;
- the cooldown prevents trigger spam;
- the object survives the intended interactions; and
- disconnect/job-change cleanup matches the game-mode settings.

## Next steps

Move operator-tunable values such as `KickForce` into a typed [content configuration](/docs/developers/systems/config-resolution) if server owners should change them without editing the prefab.
