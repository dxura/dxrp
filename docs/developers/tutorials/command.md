---
title: Command
description: Create an automatically discovered chat command, handle it on the host, and optionally persist data.
order: 5
---

This tutorial creates `/visits`, a host-handled command that increments a persistent counter for the calling player.

## 1. Implement `ICommand`

Create `VisitsCommand.cs`:

```csharp
using System.Threading.Tasks;

namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon;

public sealed class VisitsCommand : ICommand
{
    private const string Prefix = "my-addon:visits:";

    public string Command => "visits";
    public string Help => "Shows how many times you have used /visits";

    public bool ExecuteHost( Player caller, string[] args, string raw )
    {
        if ( !caller.IsValid() )
        {
            return false;
        }

        _ = IncrementAsync( caller );
        return true;
    }

    private static async Task IncrementAsync( Player caller )
    {
        var key = Prefix + caller.SteamId;
        var data = await ServerApiClient.GetStoreJson<VisitData>( key )
            ?? new VisitData();

        data.Count++;
        await ServerApiClient.SetStoreJson( key, data );

        await GameTask.MainThread();
        if ( caller.IsValid() )
        {
            caller.SendMessage( $"Visit count: {data.Count}" );
        }
    }

    public sealed class VisitData
    {
        public int Count { get; set; }
    }
}
```

Commands are found through `TypeLibrary`; there is no registration call. `ExecuteHost` returns immediately while the Store work continues asynchronously.

## 2. Understand the thread switch

Backend calls can resume away from the game thread. `await GameTask.MainThread()` switches back before touching the `Player` component. Check `caller.IsValid()` again because the player may disconnect during the request.

## 3. Test it

Run `/visits` several times:

- the count should increase;
- another player should have a separate key;
- an authorized server should retain the value across restarts; and
- local development without authorization should reset because it uses the in-memory Store mock.

Operators can inspect keys under `my-addon:visits:` in the portal Store. Prefix every key with your addon identifier so it cannot collide with another addon.

## 4. Add a permission if needed

For a staff-only command, declare a permission such as `view_visits` on the revision, then add:

```csharp
public string[] RequiredPermissionIds => ["my-addon.view_visits"];
```

The command system checks it before `ExecuteHost`. See [Permissions](/docs/developers/systems/permissions) for host-side checks inside other kinds of actions.

The full Store API, limits, expiry, and failure behaviour are covered under [Persistence](/docs/developers/systems/persistence).
