---
title: Configuration
description: How a content entry's or addon's base/override config JSON merges into the typed config object your code reads.
order: 2
---

A [content entry](/docs/developers/publishing/content-entries) (or an addon as a whole) can carry a JSON config, set once by you as a default and optionally overridden once by a game mode owner. This page covers how that resolves into an actual object your component reads.

## Two configuration systems

Do not confuse addon configuration with the server's global game configuration:

| System | Read it with | Changes when |
| --- | --- | --- |
| Server configuration | `Config.Current.Game` | An operator edits the server override |
| Addon/content configuration | `Config.Current.Addon<T>()`, `Content<T>()`, or `BaseEntity.GetConfig<T>()` | The active game mode or its addon mapping changes |

`IConfigEvents.OnConfigOverride()` is about the first system. `IGameEvents.OnGameModeUpdated()` is the event to watch when cached addon state depends on the second. It fires at startup, on every game mode save, and on clients when the host's synced game mode arrives, so rebuild cached state from the current game mode rather than applying a diff. An addon added to a running game mode mounts after that event, so also read the current config in `OnStart`. See [Lifecycle](/docs/developers/hooks/events/lifecycle).

The game mode is synced from the host. Changing `Config.Current.GameMode` in code (adding a job, a market item, or an equipment row) only changes the machine that ran it, and the next sync overwrites it. If an addon registers rows at runtime, every machine must build the same rows with the same IDs: derive the IDs from a fixed value (for example a hash of your addon ID and a key), never `Guid.NewGuid()`.

## The three addon-config layers

Config resolves from up to three layers, each optional, each only needing to specify the keys it wants to change:

1. **Fallback**: a hardcoded default your own C# code can supply inline, if you want a safety net independent of anything set on the portal.
2. **Base config**: the JSON you (the addon author) set on the content entry (`BaseConfig`) or on the whole revision (`GlobalBaseConfig`). This is your shipped default.
3. **Override**: JSON a game mode owner set for their specific server (`ConfigOverride` per content entry, `GlobalConfigOverride` per addon). This is operator customization on top of your default.

These are deep-merged in that order. Object keys from a later layer overwrite matching keys from an earlier one, recursively; anything not mentioned in a later layer falls through untouched. The merged JSON is then deserialized (case-insensitively) into whatever config class your code asks for.

For example, an author can publish this base config:

```json
{
  "MaxUses": 5,
  "Effects": {
    "Enabled": true,
    "Volume": 0.8
  }
}
```

An operator can then supply this override:

```json
{
  "MaxUses": 10,
  "Effects": {
    "Volume": 0.4
  }
}
```

The result keeps `Effects.Enabled`, replaces `Effects.Volume`, and replaces `MaxUses`.

## Reading it from your component

Define a plain config class:

```csharp
public class MyEntityConfig
{
    public int MaxUses { get; set; } = 5;
    public float Cooldown { get; set; } = 1.5f;
}
```

If your component derives from `BaseEntity`, call the built-in helper: it already knows which content entry spawned it:

```csharp
var config = GetConfig<MyEntityConfig>();
```

Otherwise, resolve it directly against a content ID or an addon ID:

```csharp
var contentConfig = Config.Current.Content<MyEntityConfig>( contentId );
var addonConfig   = Config.Current.Addon<MyAddonConfig>( addonId );

// with an inline fallback as the lowest layer:
var withFallback = Config.Current.Content<MyEntityConfig>( contentId, new MyEntityConfig() );
```

No manual JSON parsing is needed. By the time you get the object back, the base config and any operator override are already merged in. Results are cached per content/addon ID and only recomputed when the game mode changes.

Use the runtime mapping ID for content config and the stable platform addon ID for addon config—not a display name or text identifier. A `BaseEntity` already carries the correct mapped content ID, which is why `GetConfig<TConfig>()` is safer there. A `BaseSystem` receives both identities through its `Context`.

## Choose content-level or addon-level config

- Use **content-level config** when the value belongs to one entity/equipment definition, such as maximum uses or damage.
- Use **addon-level config** when every part of the addon shares the value, such as a feature toggle or external integration setting.

Avoid storing the same setting at both levels. If a per-content value falls back to an addon-wide default, encode that rule explicitly in your typed configuration rather than making operators guess which one wins.

## Where each layer actually gets set

- `BaseConfig` / `GlobalBaseConfig`: set by you, the addon author, on the portal when editing a content entry or publishing a revision. There's no example JSON file shipped in this repo for this; it's authored directly through the portal's JSON editor as part of your addon's revision data, not a file in your addon's code/assets folders.
- `ConfigOverride` / `GlobalConfigOverride`: set by the server operator from the portal's game mode manager after attaching your addon. This is their tuning layer on top of your defaults. See [Game Modes](/docs/operators/operations/game-modes) for the operator-facing workflow.

## Config vs. `[Property]` vs. persistence

Three different mechanisms, easy to mix up:

- **`[Property]` fields** are editor/prefab-time values, and the only place for asset references such as sounds and models.
- **`BaseConfig`/`ConfigOverride`** (this page) is for portal-authored settings shape: plain data values defined once by you and optionally tuned once by an operator, resolved fresh from the game mode each time.
- **[Persistence](/docs/developers/systems/persistence)** is for data *your addon writes at runtime*: player progress, saved state, counters. Not settings anyone authors up front.

## Failure behaviour

Missing IDs and invalid JSON resolve to a new `TConfig` (or the fallback object you supplied) and log a warning where parsing/deserialization failed. Design useful C# defaults and validate the same JSON in a draft revision before publishing.
