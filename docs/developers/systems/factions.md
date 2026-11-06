---
title: Factions
description: Granting faction experience, moving treasury money, reading upgrades and reacting to faction events from game code.
order: 7
---

Factions are persistent, player-run groups stored by the DXRP backend and shared by every server on a network. `FactionSystem` (`game/Code/Systems/RP/FactionSystem*.cs`) holds the networked state and is the only thing game code should talk to. Everything below runs **on the host**; the calls assert `Networking.IsHost`.

## How it fits together

- **The backend owns state and invariants.** Balances, membership, the leader, rank checks and upgrade levels are changed inside a per-faction database lock, so two servers can't double-spend a treasury, overfill a faction or buy the same upgrade level twice.
- **The game server owns rules.** Prices, member caps, the level curve and upgrade definitions come from the server's config and are sent with each request.
- **Results, not exceptions.** Every mutation returns a `FactionError`. `None` means it worked; anything else is a rule the backend rejected (for example `InsufficientFunds` or `FactionFull`).

## Experience

```csharp
// Grant to a faction directly...
FactionSystem.Instance.AddExperience( factionId, 25 );

// ...or to whatever faction a player is in (no-op if they aren't in one).
FactionSystem.Instance.AddExperience( player, 5 );
```

`AddExperience` is cheap to call often, for example from an entity that ticks every few seconds. Grants are batched per faction and saved every **Experience save interval** seconds (`FactionExperienceFlushInterval`, 10 by default). A failed save keeps the experience queued for the next flush instead of dropping it. The faction's **Training** (`experience_boost`) upgrade is applied automatically; pass `applyBoosts: false` to skip it.

Factions also earn a baseline without any extra code: every pay cycle, each member paid while not AFK adds **Experience per payday** (`FactionExperiencePerPayday`, 10 by default; 0 disables).

Levels follow the config curve. Reaching level 2 costs **Experience for level 2**, and each later level costs **Level growth** times the one before, up to **Maximum level**. `GetLevelProgress( faction )` returns the experience into the current level and the amount needed for the next, which is what the Overview tab shows.

## Treasury

Members deposit from and withdraw to their wallet in the Treasury tab. Withdrawing needs the *Withdraw funds* permission. Game code can move treasury money directly:

```csharp
// Credit: territory income, rewards, payouts.
await FactionSystem.Instance.AddFunds( factionId, 5_000, "Territory income" );

// Debit: upkeep, fines, purchases. Fails rather than going negative.
var error = await FactionSystem.Instance.TakeFunds( factionId, 20_000, "Bought a safehouse" );
if ( error != FactionError.None )
{
	return; // Don't hand out whatever was being paid for.
}
```

Every movement writes a ledger entry in the same transaction as the balance change. The ledger is kept when a faction is disbanded, so staff can still trace where its money went.

Money requests are idempotent: each carries a request id that becomes the ledger entry's id, and a lost response is retried with the same id. If the outcome still can't be confirmed, nothing is refunded or paid out. The player is told it's pending and a `FactionMoneyUnknown` audit event is written for staff. `AddFunds` and `TakeFunds` return `Conflict` in that case, so treat anything other than `None` as not paid. Members see the last 50 entries in the Treasury tab, and the backend serves them from `GET v1/server/faction/{id}/transactions`. Pass `FactionTransactionType.Income`, `Expense` or `Adjustment`, plus an optional `playerId`, so the history reads correctly.

## Upgrades

Upgrades are defined in config under **Systems → Faction → Progression → Upgrades** and are bought from the treasury by members with the *Buy upgrades* permission. Each definition has:

| Field | Meaning |
| --- | --- |
| Key | Stable id. Changing it orphans levels already bought. |
| Maximum level | How many times it can be bought. |
| Base cost / Cost growth | The first level costs Base cost; each later level costs Cost growth times the one before. |
| Value per level | What one level grants. Its meaning depends on the key. |
| Required faction level / Required level step | Faction level needed for the first level, plus the step for each further level. |

These keys have built-in effects, and all of them are in the default config:

| Key | Default name | Effect per level |
| --- | --- | --- |
| `member_slots` | Member Slots | +Value per level member slots on top of **Base member slots** (default +2). |
| `experience_boost` | Training | +Value per level to experience earned (default 0.1 = +10%). |
| `salary_bonus` | Payroll Perks | +Value per level to members' salary, before tax (default 0.05 = +5%). |
| `member_dues` | Member Dues | The treasury earns Value per level of every salary paid to members (default 0.02 = 2%). It's a bonus on top of pay, not taken from it. |
| `spawn_armor` | Field Kit | Members respawn with Value per level armor (default 10), capped at the server's max armor. |

Any other key is a custom upgrade. Read it from your own code:

```csharp
var level = FactionSystem.Instance.GetUpgradeLevel( factionId, "armory" );
var value = FactionSystem.Instance.GetUpgradeValue( factionId, "armory" ); // level x Value per level
```

## Events

```csharp
FactionSystem.LevelChanged += ( factionId, oldLevel, newLevel ) => { /* unlock something */ };
FactionSystem.UpgradePurchased += ( factionId, key, level ) => { /* apply a custom upgrade */ };
FactionSystem.BalanceChanged += ( factionId, balance ) => { /* update a display */ };
```

These fire on the host for changes made by that server. When another server or the portal changes a faction, the backend tells every online server to reload it (a `faction_changed` server action), so the change shows up within seconds but doesn't fire these events. The periodic reload (**Refresh interval**, 300 seconds by default) is the fallback if that notification is missed.

Notifications are merged over about a second, so a burst of changes to one faction (income ticks, several deposits) reaches each server once. Experience grants aren't broadcast at all, because they're too frequent. Portal changes that depend on config are corrected by the next server that loads the faction: an experience grant (the level is recalculated from that server's curve) or an upgrade level set by staff (the member cap is recalculated). Levels never go down, so servers on different curves can't keep overwriting each other.

## Banner

Members with **Manage** permission draw the banner in-game from **Settings**, using the same 512x512 canvas as paintings (`PaintCanvas`). The client only sends brush strokes; the host draws them onto the current banner with `PaintStrokes.Render` and uploads the PNG, so no image made outside the game can become a banner. The backend checks it's a 512x512 PNG and stores one file per faction.

Only the host talks to the API. `FactionBannerCache` keeps each faction's image (or "none") until a change clears it; players ask the host, which sends the bytes over RPC. A change on this server clears the cache everywhere through the mutation result, and a change on another server or in the portal clears it through the `faction_changed` action (`BannerChanged`). Show a banner with `<FactionBannerImage FactionId=... />`, which stays empty until the image arrives and gets the `loaded` class when it does.

## Membership and ranks

Role **order** is seniority: lower is more senior. The **leader** sits above every role and implicitly holds every permission. They can't be kicked, and they must hand leadership to another member before leaving. If they leave as the last member, the faction disbands.

Members can only kick or re-role people strictly below them, can only create or edit roles below their own, and can only grant permissions they hold themselves. The backend enforces all of this, and the UI mirrors it with `FactionSystem.CanManageMember`, `Outranks` and `GetPermissions`.
