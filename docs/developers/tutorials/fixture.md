---
title: Fixture
description: Build a persistent world object that staff place and remove in-game with the placement tool.
order: 4
---

A fixture is a prefab that operators place directly in the world instead of hard-coding into a map or game mode. DXRP tracks every placement (position, rotation, per-instance config) in server-side fixture state, and re-spawns all of them whenever the game mode or an addon package changes. This tutorial builds a simple street lamp fixture with an operator-tunable brightness value, then places it with the in-game placement tool.

## 1. Write the config and component

A fixture's tunable values live in a plain config class, not hard-coded on the component. The component implements `IFixture<TConfig>`, which needs no fields of its own — the placement system already knows a placement's content id and config override, and passes them straight into `Configure(...)`, which resolves the merged config and hands it to you. Create `StreetLampFixtureConfig` and `StreetLampFixture`:

```csharp
namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon;

public sealed class StreetLampFixtureConfig
{
    public float Brightness { get; set; } = 1f;
}

public sealed class StreetLampFixture : Component, IFixture<StreetLampFixtureConfig>
{
    [Property]
    public required PointLight Light { get; set; }

    public void OnFixtureConfigured( StreetLampFixtureConfig config )
    {
        Light.Brightness = config.Brightness;
    }
}
```

`IFixture<TConfig>.Configure(contentId, configOverride)` normalizes the override, resolves the merged config, and calls `OnFixtureConfigured(config)` with the result — implement that method to react to it. The placement system calls `Configure(...)` directly when it spawns a placement, before the object network-spawns, so `OnFixtureConfigured` already has everything it needs by the time the fixture exists in the world. If you need the resolved config again later, store it in your own field — the interface doesn't keep one for you.

## 2. Build the prefab

Create the prefab under your addon's assets folder:

```
fixtures/street-lamp/street-lamp.prefab
```

On the root object, add:

- `StreetLampFixture`;
- a `PointLight`, assigned to the `Light` property; and
- a model renderer for the lamp post.

The placement tool derives the ghost preview's bounding box from a `ModelRenderer`/`SkinnedModelRenderer` on the prefab (falling back to non-trigger colliders), so make sure at least one of those exists on the root or a child.

## 3. Create the content entry

In a draft revision, **Add Content**:

| Field | Value |
| --- | --- |
| Type | `Fixture` |
| Name | `Street Lamp` |
| Primary Reference | `fixtures/street-lamp/street-lamp.prefab` |
| Grouping | `Lighting` (optional — categorizes the entry in the placement tool's dropdown) |
| Base Config | `{ "brightness": 1.0 }` |

The primary reference must end in `.prefab` and be relative to the published package/bundle root — entries without a valid `.prefab` reference are skipped by fixture discovery. Save the draft.

## 4. Add it to a game mode

Attach the draft addon to a test game mode and include the `Street Lamp` content entry. An operator can override the label or `Base Config` at this mapping level — that override applies to every placement of this fixture on that game mode, layered on top of `Base Config`.

Choose **Package** or **Launcher** to match how the revision is delivered, and sync the test server.

## 5. Place it with the placement tool

The placement tool requires the `tool.placement` permission (granted to admin ranks by default).

1. Open the build menu with **Q**.
2. In the Tools tab, select **Placement** (grouped under **Admin**).
3. Pick `Street Lamp` from the **Fixture** dropdown.
4. Aim where you want the lamp. The tool draws a wireframe preview — white while floating at the tool's **Distance**, green once it settles against a surface. Enable **Fit to Surface** to sit the lamp flush on whatever you're aiming at instead of floating at the raw aim point.
5. Press **R** to rotate the preview by **Rotation Step** degrees, or drag the **Rotation** value directly.
6. To override this placement's config, open **Edit Config** and change `brightness` in the JSON editor — this override applies only to the fixture placed next, not to existing ones.
7. **Left click** places the fixture at the settled preview transform. **Right click** removes whatever fixture you're aiming at, regardless of the tool's placement distance.

Placements persist per server in fixture state, so they survive restarts and are re-spawned automatically after game-mode or addon changes.

## Config resolution order

A placed fixture's config is resolved by merging JSON objects in this order, each layer overriding matching keys from the last:

1. the content entry's **Base Config** (author default);
2. the game mode's content mapping override (operator default for that game mode); and
3. the individual placement's config override, set via the placement tool's **Edit Config** popup at the moment it was placed.

`OnFixtureConfigured(config)` runs with the final merged result, so `StreetLampFixture` never needs to know which layer a value came from.

## Next steps

Read [Content Entries](/docs/developers/publishing/content-entries) for the full field reference and update-safety rules that apply to every content type, including fixtures.
