---
name: dxrp-content
description: Build DXRP addon content in code: entities, equipment, systems, fixtures, phone apps, plus code-only services, chat commands and tab menu pages. Use when implementing any new addon feature; covers which base class or interface to use, the prefab it needs, and the .dxaddon entry that exposes it.
---

# Creating DXRP content

Every feature is code (and usually a prefab), then, if operators should select it, a content entry in the `.dxaddon` (see the `dxrp-dxaddon` skill). Working examples of each live in this SDK under `Code/Example/` and `Assets/`. For the full walkthroughs use the `dxrp-docs` skill on `developers/tutorials/`.

## Pick the kind

| Want | Build | Content entry |
| --- | --- | --- |
| Always-on logic, event hooks | `[AddonService]` `Component`, implement `IGameEvents` hooks | None |
| Chat command | Class implementing `ICommand` (`Command`, `Help`, `ExecuteHost`) | None |
| Tab menu page/action | `ITabMenuSectionProvider` registering a `TabMenuSectionDefinition` | None |
| Spawnable world object | `BaseEntity` subclass on a networked prefab | `Entity` |
| Item players hold/use | `InputWeaponComponent` (or other equipment base) on world + view-model prefabs | `Equipment` |
| Operator-toggled, configurable logic | `BaseSystem<TConfig>` | `System` |
| Persistent object staff place in the world | `Component, IFixture<TConfig>` on a prefab | `Fixture` |
| Phone app | Razor `@inherits PhoneApp` with `[PhoneApp( id, title, icon )]` | `App` |

Everything is discovered by reflection: no registration lists.

## Conventions

- Namespace under your addon, e.g. `Dxura.RP.Game.Addons.MyOrg.MyAddon`, so `Dxura.RP.Game` and `Dxura.RP.Game.Addons` resolve without usings. UI types need `using Dxura.RP.Game.UI;`.
- Prefix every ID you own with the addon identifier: Store keys (`my-addon:visits:`), tab ids (`my-addon.info`), app ids (`my-addon.coin-flip`), cooldown ids.
- Operator-tunable values go in a config class read via `BaseSystem<T>.Config`, `IFixture<T>.OnFixtureConfigured`, or `BaseEntity.GetConfig<T>()`, with defaults in `BaseConfig`. Not hard-coded, not `[Property]` only.
- Host authority: game state changes on the host (`Networking.IsHost`, `ExecuteHost`). Physics on entities: the local player takes ownership (`GameObject.Network.TakeOwnership()`) before changing a `Rigidbody`.
- After an `await` on a backend call, `await GameTask.MainThread()` and re-check `IsValid()` before touching components.
- Don't use `.UseHitboxes()` on traces; hitboxes are disabled on dedicated servers.
- Don't put `[AddonService]` on a `System` content type; pick one lifecycle owner.

## Prefabs

- Entity root: networked (`NetworkMode` object), `BaseEntity` subclass with `Rigidbody` assigned, a physics collider, a model renderer. Root tags: `entity` (required), `restricted_entity` (owner-only interaction, use for anything valuable), `pocket_item`, `occludable`, `hands_interact`.
- Fixture: needs a `ModelRenderer`/`SkinnedModelRenderer` (or non-trigger collider) for the placement ghost bounds.
- Prefer creating prefabs in the s&box editor. If writing JSON by hand, copy an existing prefab and give every `__guid` a fresh value.

## Phone apps

- Created when opened, deleted when left; collapsing the phone keeps it. Keep lasting state in a static/service, not the panel.
- `[StyleSheet]` loads `<Name>.razor.scss`; select the root by class name. Lay out with flex.
- Badges: `PhoneApps.Badges["id"] = () => count;` Status bar: subclass `PhoneStatusIcon` with `AppId`.
- Only appears when the game mode includes the App content with **Installed by default**.

## Done means

1. Code compiles in the editor and the feature is discovered (log line, menu entry, app tile, spawn menu).
2. The `.dxaddon` entry exists with a fresh, stable `Id` and the exact reference.
3. Tested locally via editor injection; host and a second client both behave.
