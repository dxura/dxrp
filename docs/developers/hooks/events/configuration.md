---
title: Configuration Events
description: React when server configuration is overridden or fully applied for a session.
order: 2
---

Implement `IConfigEvents` on an addon service or another scene component that needs to react to configuration changes:

```csharp
public interface IConfigEvents : ISceneEvent<IConfigEvents>
{
    void OnConfigAppliedHost() {}
    void OnConfigOverride() {}
}
```

## `OnConfigOverride`

`OnConfigOverride()` fires for everyone whenever the server's configuration override JSON changes. Use it to refresh client and host behavior that reads the current override.

The [Servers](/docs/operators/operations/servers) reference explains where operators change the server override. [Configuration](/docs/developers/systems/config-resolution) explains how addon and content configuration layers resolve.

## `OnConfigAppliedHost`

`OnConfigAppliedHost()` fires once on the host after configuration has finished applying for the session. Use it for one-time setup that depends on the final resolved configuration.

Do not use it for client-only UI initialization; it does not fire on clients.
