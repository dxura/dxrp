---
title: Phone App
description: Build a phone app, ship it as App content, and add a home screen badge and status bar icon.
order: 7
---

This tutorial builds a small **Coin Flip** app for the in-game phone, exposes it as `App` content, and adds it to a game mode.

## 1. Create the app

An app is a Razor panel that inherits `PhoneApp` and carries `[PhoneApp( id, title, icon )]`. Create `CoinFlipApp.razor` in your addon code folder:

```razor
@namespace Dxura.RP.Game.Addons.MyNetwork.MyAddon
@using Dxura.RP.Game.UI
@using Sandbox.UI
@inherits PhoneApp
@attribute [StyleSheet]
@attribute [PhoneApp( "my-addon.coin-flip", "Coin Flip", "toll", Order = 100, Color = "#ffd60a" )]

<root class="flex flex-col items-center justify-center">
    <label class="result">@_result</label>
    <div class="flip" onclick="@Flip">
        <label>Flip</label>
    </div>
</root>

@code
{
    private string _result = "?";

    private void Flip() => _result = Random.Shared.Next( 2 ) == 0 ? "Heads" : "Tails";

    protected override int BuildHash() => _result.GetHashCode();
}
```

| Attribute value | Meaning |
| --- | --- |
| `id` | Stable app ID. It is the content entry's primary reference, so prefix it with your addon identifier and never change it |
| `title` | Home screen label; plain text or a localisation token such as `"#my_addon.app.coin_flip"` |
| `icon` | A [Material Icons](https://fonts.google.com/icons) name, drawn white on the tile |
| `Color` | Tile colour as a CSS colour; grey if unset |
| `Background` | CSS background for the tile (e.g. a gradient), used instead of `Color` |
| `Image` | Image path or URL drawn on the tile instead of the icon |
| `Order` | Home screen position, lowest first; built-in apps use 0–100 |

Apps are found by reflection, so there is nothing to register.

## 2. Style it

`@attribute [StyleSheet]` loads `CoinFlipApp.razor.scss` from beside the component. Select the root by the class name:

```scss
CoinFlipApp {
  padding: 16px;
  gap: 24px;
  color: white;

  .result {
    font-size: 40px;
  }

  .flip {
    padding: 12px 32px;
    border-radius: 24px;
    background-color: #ffd60a;
    color: black;
    cursor: pointer;
  }
}
```

The host sizes the app to the phone screen. Lay it out with flex so it fits every phone size.

## 3. Handle the lifecycle

An app is created when it's opened and deleted when the player leaves it, either for the home screen or another app. Collapsing the phone keeps it. Override the hooks when you need them:

```csharp
public override void OnOpened()
{
    // Brought to the front: refresh data, start polling
}

public override void OnClosed()
{
    // Leaving: stop work, save drafts; the panel is deleted right after
}
```

Keep anything that must survive closing (drafts, history, unread state) in a static class or service, not on the panel.

If the game mode removes the app while it's open, the phone returns to the home screen.

## 4. Create the content entry

Add an `App` entry to your `.dxaddon` `Contents`, or in a draft revision select **Add Content**:

| Field | Value |
| --- | --- |
| Type | `App` |
| Name | `Coin Flip` |
| Primary Reference | `my-addon.coin-flip` |

The primary reference is the app ID from `[PhoneApp]`, not a path. Then **Import Definition** (if you used the `.dxaddon`) and save the revision.

A revision can suggest how game modes include the app. Add it under `Suggestions.Content` in your `.dxaddon`:

| Field | Value |
| --- | --- |
| `Content` | The app's content ID |
| `InstalledByDefault` | `true` |
| `Price` | `0` |

## 5. Add it to a game mode

Only apps a game mode includes are on the phone. In the game mode's **Content** tab, map the `Coin Flip` entry, open it, and turn on **Installed by default**. Apps that aren't installed by default will come from the app store, which isn't built yet, so they don't appear for now.

In the editor, your `.dxaddon` is injected into the local game mode automatically, so the app appears without the portal steps.

## 6. Test the app

Open the phone and confirm:

- **Coin Flip** appears on the home screen in the expected position;
- the icon and tile colour resolve;
- flipping updates the result;
- leaving and reopening the app starts it fresh;
- collapsing and reopening the phone keeps it; and
- unmapping the content, or turning off **Installed by default**, removes it.

Publish it as an s&box package and set the addon's S&box Identifier so the addon mounts on the active game mode.

## 7. Show a badge

Add a count to the home screen icon through `PhoneApps.Badges`. It's read every frame, so return a stored value:

```csharp
PhoneApps.Badges["my-addon.coin-flip"] = () => CoinFlipStats.Unseen;
```

Register it once, for example from a client-side `[AddonService]` when it starts. Return `0` for no badge.

## 8. Show a status bar icon

Subclass `PhoneStatusIcon` to show an icon in the phone's status bar, visible even with the phone collapsed:

```csharp
public sealed class CoinFlipStatusIcon : PhoneStatusIcon
{
    public override string Icon => "toll";
    public override bool IsActive => CoinFlipStats.Unseen > 0;
    public override string? AppId => "my-addon.coin-flip";
}
```

`AppId` hides the icon when the game mode doesn't include the app. `Side` picks `Notification` (beside the clock, something waiting) or `Activity` (beside the battery, something happening now). Members are read every frame, so keep them cheap.

## 9. Pick a gallery photo

To let the player choose a photo from their gallery, ask the host for the media picker. The gallery returns the file to a fresh instance of your app through `OnMediaPicked`, before `OnOpened`:

```csharp
private void ChoosePhoto() =>
    Ancestors.OfType<PhoneAppHost>().FirstOrDefault()?.PickMedia( TypeLibrary.GetType<CoinFlipApp>() );

public override void OnMediaPicked( string fileName )
{
    // Store fileName on a static or service if it must outlive the app
}
```

Because the app is recreated, keep any state you need across the pick outside the panel.
