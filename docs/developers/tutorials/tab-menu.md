---
title: Tab Menu
description: Add and control pages, actions, and default entries in DXRP's in-game tab menu.
order: 6
---

This tutorial adds a simple information page to the in-game tab menu.

## 1. Create the panel

Create `CommunityTab.razor` in your addon code folder:

```razor
@namespace Dxura.RP.Game.UI
@inherits PanelComponent

<div class="p-6">
    <h1>Community</h1>
    <p>Welcome to our server.</p>
</div>
```

Razor UI is compiled with the rest of the addon code. Keep its namespace consistent with the provider.

## 2. Register the page

Create `CommunityTabProvider.cs`:

```csharp
namespace Dxura.RP.Game.UI;

public sealed class CommunityTabProvider : ITabMenuSectionProvider
{
    public void RegisterTabMenuSections( TabMenuSectionRegistry registry )
    {
        registry.Register( new TabMenuSectionDefinition
        {
            Id = "my-addon.community",
            Label = "Community",
            Icon = "groups",
            PanelType = typeof( CommunityTab ),
            Order = 70
        } );
    }
}
```

Providers are discovered automatically. Prefix the ID with your addon identifier because every addon shares one registry.

`Label` may be plain text or a localisation token such as `"#my_addon.tab.community"`. `Icon` is a [Material Icons](https://fonts.google.com/icons) icon name. Entries are sorted by placement, then `Order`, then label.

## 3. Test the page

Open the tab menu and confirm:

- **Community** appears in the expected order;
- the Material icon resolves;
- selecting it creates the Razor panel;
- reopening the menu does not duplicate the entry; and
- the page behaves at different screen sizes.

This is a code-only feature, so it does not need a content entry. For Launcher delivery, upload the `Code/` folder and select **Launcher** on the game-mode addon. For s&box Package delivery, publish/bind the package and select **Package** so the addon mounts on the active game mode.

## 4. Control when a tab is available

Use `CanShow` to hide an entry dynamically. Use the disabled flags when it should remain visible but unavailable to a dead or restricted player:

```csharp
registry.Register( new TabMenuSectionDefinition
{
    Id = "my-addon.management",
    Label = "Management",
    Icon = "admin_panel_settings",
    PanelType = typeof( ManagementTab ),
    Order = 80,
    DisabledWhenDead = true,
    DisabledWhenRestricted = true,
    CanShow = () => Player.Local.IsValid()
        && RankSystem.HasPermission( Player.Local.SteamId, Permission.HandleTickets )
} );
```

`CanShow` is evaluated while the menu is open, so it can depend on the local player's current state.

## 5. Add an action

An entry without `PanelType` is an action. It can copy text, run code when clicked, or do both:

```csharp
registry.Register( new TabMenuSectionDefinition
{
    Id = "my-addon.website",
    Label = "Community website",
    Icon = "language",
    Placement = TabMenuEntryPlacement.Bottom,
    Order = 30,
    CopyText = () => "https://example.com",
    CopiedLabel = "Link copied"
} );
```

Use `OnClick` for custom behaviour. The context exposes the menu panel and `CloseMenu`:

```csharp
OnClick = context =>
{
    // Run your addon action here.
    context.CloseMenu();
}
```

`Placement = TabMenuEntryPlacement.Bottom` puts an entry in the footer.

## 6. Remove or replace default entries

Native DXRP entries are registered before addon providers. Remove one by its ID:

```csharp
public void RegisterTabMenuSections( TabMenuSectionRegistry registry )
{
    registry.Remove( "dashboard" );
    registry.Remove( "credits" );
    registry.Remove( "support" );
}
```

The built-in IDs are:

| ID | Entry |
| --- | --- |
| `dashboard` | Dashboard |
| `rules` | Rules |
| `players` | Players |
| `jobs` | Jobs |
| `market` | Market |
| `governance` | Governance |
| `faction` | Faction |
| `credits` | Credits (footer) |
| `discord` | Discord action (footer) |
| `support` | Support action (footer) |

To replace an entry, remove it and register a new definition with the same ID:

```csharp
registry.Remove( "dashboard" );

registry.Register( new TabMenuSectionDefinition
{
    Id = "dashboard",
    Label = "Welcome",
    Icon = "home",
    PanelType = typeof( CommunityTab ),
    Order = 0
} );
```

`Remove` returns `false` when no matching entry exists, including when another provider removed it first.

The registry is rebuilt when the tab menu starts and whenever it opens. Keep registration inside `RegisterTabMenuSections`; do not retain and modify the registry later.
