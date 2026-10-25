---
title: Networking
description: Where to put state and RPCs in an addon, which patterns survive late package mounting, and the network flags that fail silently.
order: 5
---

DXRP is host-authoritative. The host owns money, jobs, statuses, and every piece of shared state; clients render it and ask the host to change it. This page covers the patterns that work for addon code, which is compiled separately and often mounted after core has started (see [Addon Load Order](/docs/developers/hooks/events/load-order)).

For the s&box primitives themselves (`[Sync]`, `[Rpc.*]`, `NetworkSpawn`), see the official [s&box documentation](https://sbox.game/dev/doc/).

## Choose where state lives

| You need | Use | Core example |
| --- | --- | --- |
| A value every client can read, written by the host | `[Sync( SyncFlags.FromHost )]` property on a networked object | `Player.Restricted`, `HealthComponent.MaxHealth` |
| To react when that value changes | Add `[Change( nameof( OnXChanged ) )]` | `HealthComponent.State` → `OnPlayerLifeStateChanged` |
| A keyed collection | `NetDictionary<TKey, TValue>` / `NetList<T>` with `[Sync( SyncFlags.FromHost )]` | `PartySystem.Parties`, `Player.Statuses` |
| Private data for one player | A host → owner push (see below) | `MiningClient.PushState` |
| A one-off effect (sound, particles) | `[Rpc.Broadcast( NetFlags.HostOnly )]` | `OreNode.ImpactOnHit` |

A `[Sync]` property without `[Change]` replicates silently: nothing tells you it changed, so reacting to it means polling. Add `[Change]` when you own the property.

Changing a plain C# object stored in a `[Sync]` property, such as `Config.Current.GameMode.Jobs.Add( ... )`, is not a sync. It changes the local copy only, and the next update from the host replaces it.

## Where to put RPCs

An RPC must target an object that exists, with the same identity, on both the caller and the receiver. Use one of these two patterns.

### Instance RPCs on a host-spawned networked object

Entities, NPCs, and other prefabs that the host clones and calls `NetworkSpawn()` on exist with the same identity on every machine. Client → host requests belong on them:

```csharp
public sealed class VendorNpc : Component
{
    [Rpc.Host( NetFlags.Reliable )]
    public void BuyHost( string itemId )
    {
        var player = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
        if ( !player.IsValid()
            || WorldPosition.Distance( player.WorldPosition ) > Config.Current.Game.ReachDistance )
        {
            return;
        }

        // Validate and act on the host.
    }
}
```

Core's mining addon does this on `MinerNpc` and `ForgeFurnace`.

If your addon has no natural entity to hang RPCs on, create an **anchor**: a small prefab with your component, cloned and network-spawned by the host from your service, exposing a `static Current` set in its `OnStart`. UI code then calls `MyAnchor.Current?.DoSomethingHost( ... )`.

### Static broadcast RPCs for host → client pushes

A static method needs no object identity at all. Use it to push data to one player, filtered by connection:

```csharp
public static class MyAddonClient
{
    public static MyState State { get; private set; } = new();

    [Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
    public static void PushState( string json )
    {
        State = JsonSerializer.Deserialize<MyState>( json ) ?? new MyState();
    }
}

// On the host:
using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
{
    MyAddonClient.PushState( JsonSerializer.Serialize( state ) );
}
```

Core's `MiningClient` uses exactly this shape. The client copy is display state only; the host re-checks every value before acting on it.

### What to avoid

- **Instance RPCs on an `[AddonService]` or a `System` content component.** These are created at runtime, separately on each machine, rather than spawned by the host. Core's own `MiningSystem` carries no RPCs for this reason; its RPCs live on the NPC, furnace, and static client class.
- **Instance RPCs on a `GameObjectSystem<T>` from addon code.** Addon authors have reported them failing with `OnObjectMessage: Invalid GameObjectSystem` when the package is mounted after the scene started. Core's `Notify` uses RPCs on a `GameObjectSystem`, but core's assembly is loaded before the scene exists; yours may not be.

## Network flags that fail silently

| Flag | On | Effect |
| --- | --- | --- |
| `NetFlags.OwnerOnly` | Equipment, or anything the calling player owns | Correct: only the owner can call it |
| `NetFlags.OwnerOnly` | A host-spawned anchor, NPC, or entity | **Every non-host player is ignored**, with no error or log. The host owns the object, so only the host can call it |
| `NetFlags.HostOnly` | A broadcast or owner RPC | Says who is expected to send it. Do not treat it as authorization; see below |

Only the flagged RPC stops working, so an `OwnerOnly` mistake usually looks like a UI bug: the panel opens (its RPC has no flag) but the button does nothing.

## Validate the caller in the body

Treat every `[Rpc.Host]` as a request from an untrusted client. Resolve the player from the connection, never from a parameter:

```csharp
var player = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
```

then check, on the host, everything the client could lie about: range, ownership (`GameUtils.HasPermission`), permissions (`RankSystem.HasPermission( Rpc.Caller.SteamId, ... )`), state (`player.Restricted`, alive, job), and rate (`Cooldown.Current.CheckAndStartCooldown( $"{Rpc.CallerId}:my-addon:action", seconds )`). Core's `AdminSystem` and `Atm` follow this pattern. The [Security](/docs/developers/systems/security) page lists the money-specific rules.

## Late joiners

A client that connects after something happened only sees its current synced state. A `[Rpc.Broadcast]` sent before it connected is gone, and one that lands before the target object exists on the client is lost too; nothing retries it.

For anything that must be visible to late joiners (a status visual, a placed effect, a door state), store the state in a `[Sync]` property and build the visual from it in `OnStart` and in its `[Change]` callback. Use broadcasts only for transient effects you do not mind a late joiner missing.

## Testing

- Test with a host **and** a second client; the editor host alone hides most of these bugs.
- Join with the client after the feature has been used, to check late-joiner state.
- Attach the addon while the server is running, then use the feature from a client.
- Remember that `Cooldown.Current` is shared between host and client in a single editor session, so a cooldown started client-side can block the host's own check there. Confirm rate limits on a real client.
