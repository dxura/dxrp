---
name: dxrp-docs
description: Look up DXRP's official documentation (addons, content entries, tutorials, hooks, systems, publishing, operator portal) from the public dxura/dxrp repo. Use whenever a DXRP API, addon workflow, portal step, or game behaviour needs checking instead of guessing.
---

# DXRP docs

The DXRP docs are Markdown in the public repo `dxura/dxrp` under `docs/`, published at https://dxrp.net/docs.

## Find the page

1. If this workspace has the DXRP repo checked out (a `docs/manifest.json` next to `game/`), read the local files; they match your build.
2. Otherwise fetch the index:
   `https://raw.githubusercontent.com/dxura/dxrp/main/docs/manifest.json`
3. The manifest is a tree of sections. Each page lives at `docs/<section>/<page>`, e.g. section `developers/tutorials` + page `entity.md` is
   `https://raw.githubusercontent.com/dxura/dxrp/main/docs/developers/tutorials/entity.md`.
4. Fetch only the pages the question needs. Page front matter (`title`, `description`) says what each covers.

Links inside pages look like `/docs/developers/systems/config-resolution`; map them to `docs/developers/systems/config-resolution.md`.

## Where things are

| Need | Section |
| --- | --- |
| Create an addon, `.dxaddon` format, local testing | `developers/creating-addons.md` |
| Step-by-step: service, entity, equipment, fixture, command, tab menu, phone app | `developers/tutorials/` |
| Game events and handlers (lifecycle, config, equipment, commands, server actions) | `developers/hooks/` |
| Config layering, permissions, persistence (Store), networking, security | `developers/systems/` |
| Content entries, s&box packages, revisions, monetization | `developers/publishing/` |
| What operators see: game modes, content mapping, market, jobs | `operators/` |
| Backend HTTP API | `api/` |

## Rules

- Quote or cite the page you used (its path) when answering.
- If the docs and the game code disagree, trust the code and say so.
- Don't invent APIs. If no page covers it, say that and look at the game code (`Dxura.RP.Game` in `package.dxura.rp`) instead.
