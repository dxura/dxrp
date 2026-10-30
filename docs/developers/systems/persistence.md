---
title: Persistence
description: Two ways for addon code to save and load data that survives a restart, the Store and the local filesystem.
order: 4
---

Addon code needing data to survive a restart has two real options: **the Store**, a network-scoped API backed by the DXRP backend, or the **local filesystem**, s&box's own on-disk storage API. They solve different problems. Most addons only need the Store; read the filesystem section's caveats before reaching for it instead.

## The Store

A generic, persistent key-value API, reachable from any server-side addon code, that saves your data through the DXRP backend rather than to disk. Think of it as the addon equivalent of a small database table, without needing to run one.

### API

All of it lives on `ServerApiClient` and is safe to call from any server-side code:

```csharp
await ServerApiClient.SetStore( "myaddon:settings", jsonString );
var value = await ServerApiClient.GetStore( "myaddon:settings" );
await ServerApiClient.DeleteStore( "myaddon:settings" );
var keys = await ServerApiClient.ListStore( "myaddon:" ); // prefix filter

// JSON convenience wrappers, serialize/deserialize for you
await ServerApiClient.SetStoreJson( "myaddon:settings", myObject );
var obj = await ServerApiClient.GetStoreJson<MySettings>( "myaddon:settings" );
```

Keys are trimmed and lowercased before use, so lookups are case-insensitive. `SetStore` optionally takes an `expiresAt` timestamp; past that point the entry is treated as gone, filtered out of reads immediately and physically purged by an hourly server-side sweep.

### Scoping: namespace your own keys

The Store is scoped **per network** (every server belonging to your network shares the same key space), not per individual server process and not per player. There's no separate per-server or per-player partitioning built in. If you need that, encode it into the key yourself, for example:

```
myaddon:player:{steamId}:progress
myaddon:server:{serverId}:state
```

The built-in waypoint chat command (`game/Code/Chat/Commands/WaypointCommand.cs`) is a good reference: it prefixes every key with `commands:waypoint:`, stores each waypoint as JSON via `SetStoreJson`, and uses `ListStore( "commands:waypoint:" )` to enumerate all of them without colliding with anything else sharing the network's store.

### Limits

Enforced server-side, so calls that exceed these fail rather than silently truncating:

| Limit | Value |
| --- | --- |
| Key length | 512 characters |
| Value size | 1 MB |
| Entries per network | 250,000 total |

Updates to an existing key don't count against the entry cap, only creating a brand new key does. Keep this in mind: the Store is meant for small-to-moderate structured data (settings, saved locations, feature flags, small per-player records), not bulk storage. Operators can inspect it from the portal's [Store](/docs/operators/administration/store), and it is included in regular [backups](/docs/operators/administration/backups).

### Failure model

Every Store call is non-throwing from your addon's point of view: failures are logged and return `null` (or the default for JSON reads) rather than raising an exception. Treat a `null` result as "not found, or the call failed," not as a distinct case you need to branch on separately.

### Local/offline development

If your server isn't linked to the backend yet (no authorization token), Store calls transparently fall back to an in-memory mock with the same semantics, but nothing persists across a process restart. This means addon code using the Store can be developed and tested locally without a live backend connection.

## The local filesystem

s&box also exposes `FileSystem.OrganizationData`, a sandboxed on-disk storage API (`FileExists`, `DirectoryExists`, `CreateDirectory`, `ReadAllText`/`ReadAllTextAsync`, `WriteAllText`, `DeleteFile`, `FindFile`), scoped to a folder the engine manages for your organization. In this codebase it's used for things like a per-machine identifier file, chat title preferences, tab menu and command wheel preferences, and "seen changelog" state.

Two things worth knowing before you reach for it:

- **Every existing use in this repo is client-side.** Nothing in the game code uses `FileSystem.OrganizationData` from a dedicated server process to persist gameplay data, so there's no established in-repo pattern to copy for that case, and this codebase can't tell you whether the storage location is per-machine or shared, or whether it survives a server's game-code/addon updates.
- It only lives on the machine running that process. Unlike the Store, it isn't visible to other servers on your network, isn't included in the portal's [backup](/docs/operators/administration/backups) system, and doesn't survive a server migrating to different hardware.

Reach for the filesystem for local, disposable, single-machine data (caches, machine-local preferences), and use the Store for anything that needs to be shared across your network's servers, backed up, or trusted to survive a server move. If you're unsure which one fits, default to the Store.

For a complete implementation with async/thread-safety handling, follow the [Command](/docs/developers/tutorials/command) tutorial.
