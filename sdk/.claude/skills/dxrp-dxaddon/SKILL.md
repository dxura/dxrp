---
name: dxrp-dxaddon
description: Create or edit a DXRP addon definition (.dxaddon): addon id, content entries, global config, permissions, and suggested jobs, market items and content settings. Use when adding or changing an addon's definition, wiring new content into it, or debugging why content doesn't appear in the editor or on portal import.
---

# DXRP addon definitions (`.dxaddon`)

A `.dxaddon` is a `Dxura.RP.Game.Addons.AddonDefinition` GameResource (JSON). It is the single source of truth for an addon:

- **Editor:** every mounted `.dxaddon` with `InjectInEditor: true` is injected into the local game mode, suggestions applied, so content can be tested without the portal.
- **Portal:** **Import Definition** on the addon page reads the same file into the next revision.

Full reference: `developers/creating-addons.md` (use the `dxrp-docs` skill). Working example: `Assets/example.dxaddon` in this SDK.

## Shape

```json
{
  "AddonId": "<portal addon guid>",
  "Name": "My Addon",
  "Description": "...",
  "InjectInEditor": true,
  "GlobalConfigType": "",
  "GlobalBaseConfig": "{}",
  "Contents": [
    {
      "Id": "<new guid, never change>",
      "Name": "Bouncy Ball",
      "Type": "Entity",
      "Description": "...",
      "PrimaryReference": "addons/myorg/my-addon/entities/ball/ball.prefab",
      "SecondaryReference": null,
      "Grouping": null,
      "WorldModelPath": null,
      "WorldModelScale": null,
      "IconPath": null,
      "BaseConfig": null
    }
  ],
  "Permissions": [
    { "Identifier": "view_visits", "Name": "View visits", "Description": null, "Category": null }
  ],
  "Suggestions": {
    "Jobs": [],
    "Content": [],
    "MarketItems": []
  },
  "__references": [],
  "__version": 0
}
```

## Content `Type` and `PrimaryReference`

| Type | PrimaryReference |
| --- | --- |
| `Entity` | World prefab path (component derives from `BaseEntity`) |
| `Equipment` | World prefab; `SecondaryReference` = view-model prefab |
| `System` | Namespace-qualified type, e.g. `MyOrg.MyAddon.TipsSystem` (derives from `BaseSystem<TConfig>`) |
| `Fixture` | `.prefab` path (component implements `IFixture<TConfig>`) |
| `App` | The `[PhoneApp( id, ... )]` id, not a path |
| `Emote` | Animation sequence name |
| `Vehicle` | Reserved, unused |

Paths are relative to the **published package asset root**: no `Assets/`, no `Code/`, no org prefix that isn't really a folder.

## Suggestions

- `Content[]`: per-content game-mode defaults. `Content` (content id) and `ConfigOverride` for any type; `NameOverride`, `DescriptionOverride`, `Limit` for entities/equipment; `HealthEnabled`, `HealthAmount`, `DestroyOnDisconnect`, `DestroyOnJobChange`, `AllowOwnershipTransfer` for entities; `Price`, `InstalledByDefault` for apps. Fields that don't fit the type are ignored.
- `MarketItems[]`: `Key` (stable guid), `Content` (Entity or Equipment id), `Cost`, `Grouping`, `SortOrder`, `Color` (packed `0xRRGGBB` as an int, 16777215 = white), `Quantity`, `WhitelistJobs` (suggested job keys), `WhitelistJobTags`.
- `Jobs[]`: `Key`, `Name`, `Description`, `Color`, `Model`, `Clothes`, `JobTags`, `Salary`, `Health`, `MaxCount`, `Selectable`, `Demotable`, `VoteRequired`, `IncludeDefaultEquipment`, `Equipment` (Equipment content ids).

## Rules

- Generate fresh GUIDs for `AddonId` (use the portal addon's id once it exists), each content `Id`, and each market/job `Key`. Never reuse or change them: game modes bind to them.
- `BaseConfig` and `GlobalBaseConfig` are JSON **text** (an escaped string), not nested objects. Keys must match the config class property names.
- Set `GlobalConfigType` to the full name of the addon-wide config class to have the editor keep `GlobalBaseConfig` in sync with code defaults.
- Every suggestion's `Content` must be one of this file's contents, of a fitting type; keys must be unique. The portal rejects the revision otherwise.
- Permissions are the suffix only; the portal prefixes them with the addon identifier (`view_visits` becomes `my-addon.view_visits`). They are not injected in the editor.
- Keep assets under a folder unique to the addon, e.g. `Assets/addons/myorg/my-addon/`.
- To test the published revision instead of the local file, set `InjectInEditor` to false.

## Check

After editing, validate the JSON parses, every suggestion `Content` matches a content `Id`, and every `PrimaryReference` file or type exists in the project.
