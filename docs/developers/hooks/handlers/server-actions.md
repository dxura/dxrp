---
title: Server Actions
description: Handle advanced typed actions pushed from the portal to a running server.
order: 2
---

Portal actions—such as applying a new game mode or server configuration—reach a running server as typed `BaseServerActionDto` payloads. `IActionHandler` dispatches those payloads:

```csharp
public interface IActionHandler
{
    bool CanHandle( BaseServerActionDto action );
    void Execute( BaseServerActionDto action );
}

public abstract class ActionHandler<T> : IActionHandler where T : BaseServerActionDto
{
    public bool CanHandle( BaseServerActionDto action ) => action is T;
    public void Execute( BaseServerActionDto action ) { /* dispatches to Execute(T) */ }
}
```

Use the generic `ActionHandler<T>` base when adding a typed handler. `CanHandle` selects the action type and the base class dispatches it to the typed `Execute` implementation.

This is advanced infrastructure. Prefer an existing [event hook](/docs/developers/hooks/events/lifecycle) when the addon only needs to observe game or configuration changes. Add a server action only when the portal must initiate a new typed operation.

`UpdateGameModeActionHandler` and `UpdateServerInfoActionHandler` are the built-in implementations to reference.
