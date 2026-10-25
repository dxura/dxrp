---
title: Servers
description: Create servers, edit their configuration, monitor status, and use runtime actions and snapshots.
order: 1
---

**Where:** Open **Servers**, then select a server name.

The list is the entry point for both configuration and live operation. Use **Create New Server** to add one, or search by name or ID to find an existing server.

## Server configuration

Select **Edit** to change:

- name and description;
- maximum players;
- [map](/docs/operators/operations/maps);
- [ruleset](/docs/operators/operations/rulesets);
- [game mode](/docs/operators/operations/game-modes);
- ranks allowed through the whitelist; and
- the JSON configuration override.

The override contains only values changed from the game build's defaults. The server resets to its baseline before applying that JSON, so removing a value restores its default rather than preserving an older override. Validate and format the JSON in the editor before saving.

Configuration changes take effect on the next server restart.

The configuration editor groups settings by their gameplay area:

| Category | Examples |
| --- | --- |
| Chat | Message distance, TTS, emojis, and automatic messages |
| Cooldowns | Chat, tool, weapon, arrest, election, law, and other action timers |
| Economy | Payouts, drop behaviour, and economy-system tuning |
| Entity | Behaviour specific to individual entity systems |
| Equipment | Tools, weapons, medkits, and breach timing |
| Governance | Laws, jail, lockdown, mayor, taxes, police upgrades, raids, and hits |
| Limits | Props, lights, text, frames, doors, and wire components |
| Player | Reach, nameplates, AFK, armour, fall damage, and revive behaviour |
| Radio | Stations available to players |
| Sentinel | Anti-cheat detectors, reporting, punishment, and thresholds |
| Systems | Salary, voting, factions, autosave, jobs, money, and other core systems |
| UI | HUD, tab menu, nameplates, titles, camera, and command-wheel defaults |

## Status and players

The management page shows whether the server is active, its build version, uptime, current and peak players, and host health. The live player table includes session information such as rank, connected time, and client FPS.

## Actions

- **Broadcast** sends an announcement to everyone currently connected.
- **Restart Now**, **Restart in 2m**, and **Restart in 5m** schedule a restart. Prefer a delay while players are online.
- **Regenerate Token** creates a new authorization token, copies it, and invalidates the old one. It is shown only once.
- **Delete Server** permanently removes the portal server record.

See [Setting Up a Community Server](/docs/operators/setting-up-a-server) for the launch command that uses the authorization token.

## Snapshots

A snapshot is the saved world state for this server.

- **Download** before a risky map or content change.
- **Upload** replaces the snapshot on the next restart. The maximum upload is 50 MB.
- **Clear** starts with an empty world state on the next restart.

Snapshots are per server and cover the world. They are different from network [Backups](/docs/operators/administration/backups), which cover player and Store data.

## Permissions

Viewing and editing servers use `servers.view` and `servers.edit`; snapshots use `snapshots.manage`. Rank visibility can also limit which actions appear.
