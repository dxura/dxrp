---
title: Security
description: The rules that keep an addon from becoming an exploit, for ownership, money, async code, restricted players, and client-run minigames.
order: 6
---

A functional bug produces a bug report. A security bug produces an exploit that players find before you do, and on a roleplay server most exploits are about money. This page collects the rules that matter most for addon code. Read [Networking](/docs/developers/systems/networking) first for where RPCs live and how to identify the caller.

## The client is never the authority

Everything a client sends is a request. On the host, re-check:

- **who**: resolve the player from `Rpc.CallerId`, never from a Steam ID passed as a parameter;
- **may they**: `RankSystem.HasPermission( steamId, ... )` for staff actions, `GameUtils.HasPermission( steamId, gameObject )` for objects;
- **can they**: alive, not `Restricted`, in range, holding the right job;
- **how often**: `Cooldown.Current.CheckAndStartCooldown( $"{Rpc.CallerId}:my-addon:action", seconds )` returns `true` while still cooling down;
- **is the input sane**: lengths, ranges, finite numbers.

A client-side check is useful for hiding a button. It is not authorization. See [Permissions](/docs/developers/systems/permissions).

## Ownership tags

`GameUtils.HasPermission` returns `true` for **every player** on an object tagged `entity` unless it also has `restricted_entity`. That is intentional for garbage, dropped money, and printers (which are meant to be stealable), but it means an entity built only with the `entity` tag can be picked up, used, or removed by anyone.

| Tags | Who can interact |
| --- | --- |
| `entity` | Everyone |
| `entity` + `restricted_entity` | The owner and players they permit |
| `entity` + `restricted_entity` + `claimable_entity` | Everyone until the first player claims it |

Add `restricted_entity` to anything that holds value or must stay with its owner. See the [Entity](/docs/developers/tutorials/entity) tutorial for the full tag list.

## Money

`Player.ChargeHost` and `Player.PayHost` are the only correct way to move money. Both are host-only and return `Task<bool>`:

```csharp
if ( !await player.ChargeHost( price, "My Addon: purchase" ) )
{
    return; // not charged: insufficient funds or the backend refused
}
```

Know what the result means:

- They **do not throw** on failure; they return `false`. A `try`/`catch` around them catches nothing, and an unchecked result announces a success that never happened.
- They return `true` without moving anything when `amount` is `0` or when money is disabled on the server (`MoneyEnabled = false`). `true` means "nothing to refuse", not "money moved".
- Bank operations call the DXRP backend and can suspend for many frames. Wallet operations do not call the backend, but every operation first waits for that player's transaction lock, so any of them can suspend. Always `await` them.

### Charge first, then give, then refund on failure

Take the money before handing anything out, and undo the charge if the second half fails. Core's `Atm` follows this order:

```csharp
if ( !await player.ChargeHost( amount, "My Addon: exchange" ) )
{
    return;
}

if ( !await player.PayHost( netAmount, "My Addon: exchange", inBank: true ) )
{
    await player.PayHost( amount, "My Addon: exchange refund" );
}
```

### Every `await` is a gap

Anything can happen while an `await` is suspended: the player disconnects, another RPC from the same player runs, the round ends. Two consequences:

**Re-validate after every `await`.** A check done before the await is stale after it. Check `player.IsValid()`, range, state, and any cap again before acting on the result.

**Claim before you pay.** If several RPCs can reach the same payout, the first thing each one does must be to atomically claim it, and only the winner pays:

```csharp
// Wrong: three concurrent calls all find the ticket and all get paid.
if ( _tickets.TryGetValue( steamId, out var ticket ) )
{
    await player.PayHost( ticket.Prize, "Lottery" );
    _tickets.Remove( steamId );
}

// Right: only the call that removes the ticket pays it.
if ( _tickets.Remove( steamId, out var ticket ) )
{
    if ( !await player.PayHost( ticket.Prize, "Lottery" ) )
    {
        _tickets[steamId] = ticket; // put it back so it can be retried
    }
}
```

The same applies to iterating a snapshot of a collection and paying each entry: remove the entry, check the result of `Remove`, then pay.

### Numbers

- Amounts are `uint`. Convert from `float` deliberately and reject negative or out-of-range input before casting.
- `Math.Clamp` passes `NaN` straight through. Reject non-finite input first with `float.IsFinite( value )`.
- Multipliers and taxes are applied with float maths; round explicitly and decide in which direction.

## Restricted players

`Player.Restricted` is the master switch core uses for players in jail or in a minigame. While it is set, core:

- does not drop the wallet on death or pockets on death and job change;
- does not clear job entities on job change;
- refuses entity spawns, market purchases, and job switching;
- terminates an active hit when the hitman is restricted;
- respawns the player at jail-tagged spawn points; and
- hides the build menu and disables restricted tab menu entries.

Use it for arenas, events, and minigames instead of reimplementing each of these. Check it in your own host code before giving out money or items, set it on the host only, and always clear it on exit, disconnect, and failure paths. Core's `MinigameSystem` and jail are the references.

## God mode

`HealthComponent.IsGodMode` is a single boolean, not a count. Grant it through the `god` status (`player.AddStatus( Constants.GodStatus )`) rather than writing the flag, and only remove the status if your code added it:

```csharp
if ( !player.HasStatus( Constants.GodStatus ) )
{
    player.AddStatus( Constants.GodStatus );
    _grantedGod.Add( player.SteamId );
}
```

Some core paths (kill boxes, elevators, medkit revives) set `IsGodMode = false` directly, so do not rely on god mode surviving those.

## Client-run minigames

A minigame drawn on the client cannot be made bot-proof: the client has to know the solution to display it. Host-side checks on ownership, range, cooldown, and minimum duration do not help if the **result** is still a `bool` the client sends; a script waits out the minimum duration and reports success.

What does work:

- run the simulation on the host from the client's streamed input, and decide the result there;
- send the client only the feedback it needs to play (for example proximity), not the solution; and
- remove the "I won" RPC entirely rather than merely not calling it from your UI.

If the reward is small, accept the risk knowingly and rate-limit it instead.

## Checklist

- Every `[Rpc.Host]` resolves the caller from the connection and re-checks permission, state, range, and rate.
- Value-holding entities carry `restricted_entity`.
- Every `ChargeHost` / `PayHost` result is checked; failures refund.
- State is re-validated after every `await`; payouts are claimed before they are paid.
- Numeric input is finite and in range before it is used.
- `Restricted` is respected, set only on the host, and always cleared.
- No reward depends on a result computed by the client.
