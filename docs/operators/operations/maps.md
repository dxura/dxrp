---
title: Maps
description: Register s&box maps, define parent maps, and upload DXRP prefab data.
order: 3
---

**Where:** Open **More → Maps**.

The Maps tab contains the map definitions available to your network's servers. Registering a map here does not publish the underlying s&box package; it tells DXRP which package and prefab data to use.

## Add a map

Select **Create New Map** and provide:

- **Map Name**: the operator-facing display name;
- **S&box Identifier**: the package identifier derived from the map's s&box URL;
- **Description**: what operators should know before assigning it; and
- **Parent Map**: optional inheritance from another map definition.

For an s&box URL such as `sbox.game/author/package`, the identifier is `author.package`.

## Prefab JSON

Upload the prefab JSON produced for the map's DXRP fittings. Replacing the file changes what the server receives for that map; download or retain the previous version before overwriting production data.

## Parent maps

Use a parent when a map variant should inherit the base definition instead of duplicating it. Avoid inheritance loops and make the relationship clear in the description.

## Assign a map

After saving, open [Servers](/docs/operators/operations/servers), edit the server, select the map, and restart when convenient.

Public maps require platform approval. The portal directs approval requests to the DXRP Discord.

Map access uses `maps.view` and `maps.edit`.
