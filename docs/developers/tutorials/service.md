---
title: Service
description: Create an automatically discovered addon service that loads once and reacts to game events.
order: 1
---

This tutorial creates the smallest useful code-only addon: a service automatically created when the scene loads, with a host-side player-join hook.

## 1. Create the service

Create:

```
Code/WelcomeService.cs
```

Add:

```csharp
using Dxura.RP.Game.Addons;

namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon;

[AddonService]
public sealed class WelcomeService : Component, IGameEvents
{
    protected override void OnStart()
    {
        Log.Info( "[MyAddon] Welcome service started" );
    }

    public void OnPlayerJoined( Player player )
    {
        if ( !player.IsValid() )
        {
            return;
        }

        player.SendMessage( "Welcome to the server." );
    }
}
```

`AddonServiceRegistry` finds the attribute and creates one component on the `GameManager` root. `OnPlayerJoined` is posted on the host after the player has initialized.

## 2. Test discovery

Build the project and start a local game:

- The log line should appear once per scene.
- Joining with a client should produce one welcome message.
- Reloading the scene should create a new service for that scene, not duplicate it on the same root.

If the service does not load, check that the class is concrete, derives from `Component`, carries `[AddonService]`, and is inside the compiled addon code folder.

## 3. Publish it

This addon needs no assets or content entries. Create a draft revision, then either upload the `Code/` folder for [Launcher delivery](/docs/developers/publishing/publishing-addons) or bind the revision to the published package for [Package delivery](/docs/developers/publishing/sbox-packages). Attach the matching load method to a test game mode and verify the service again.

A code-only addon still must be present in the active build or mounted package before discovery can see it. Compiled code needs no game-mode content mapping; an s&box package must be attached to the active game mode so it can mount.

## Extend it

Implement only the lifecycle methods you need. The [Lifecycle](/docs/developers/hooks/events/lifecycle), [Configuration Events](/docs/developers/hooks/events/configuration), and [Equipment Events](/docs/developers/hooks/events/equipment) references cover the available hooks.
