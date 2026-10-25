# Maintaining the DXRP docs

Everything under this folder is published at [dxrp.net/docs](https://dxrp.net/docs). The webapp reads it straight from the `main` branch of this repository, so a merged change goes live within a few minutes with no webapp release.

## Contributing

Anyone can improve the docs:

1. Use **Edit this page on GitHub** at the bottom of any docs page, or fork this repository and edit files under `docs/`.
2. Follow the structure and page format below. New pages must be added to `manifest.json` or they won't appear.
3. Open a pull request against `main`. Once it's merged, the change shows up on the site shortly after.

## Structure

```
docs/
  manifest.json          # section tree and page filenames
  <section>/
    some-page.md          # one file per doc page
    <subsection>/
      another-page.md
    images/
      some-page/
        screenshot.png     # images used by some-page.md
```

- Each entry in `manifest.json` is a navigation section. Its `section` value is the folder path relative to this directory.
- Sections can have recursive `children`. Child sections appear as nested dropdowns in the sidebar while their full folder paths produce URLs such as `/docs/developers/tutorials/...`.
- Every page must be listed by filename in its section's `pages` array. The webapp does not scan directories at runtime.
- Adding a section: add an entry to `manifest.json` (at the root or inside `children`) and create the matching folder. `icon` is optional and accepts a [PrimeIcons](https://primeng.org/icons) class name (e.g. `pi pi-server`).
- Adding a page: create its `.md` file and add the filename to the section's `pages` array. Its filename without `.md` becomes the final URL segment, e.g. `developers/tutorials/entity.md` → `/docs/developers/tutorials/entity`.
- Keep cross-page links rooted at `/docs/`; this works both from the docs home page and when a reader lands directly on a deep link.
- When a page describes portal behavior, confirm the labels against the Angular component and backend DTO before publishing. When it describes s&box behavior, link to the matching official s&box documentation and include the installed-build caveat where UI labels can differ.

## Page format

Every page needs frontmatter with a `title` and `description`, and an optional `order` (pages within a section sort by `order` ascending, then alphabetically):

```markdown
---
title: Setting Up a Community Server
description: Create a server on the DXRP portal, configure a map, and go live on Windows or Linux.
order: 1
---

Page content starts here in Markdown.
```

## Recommended page shape

For a workflow page, answer these questions in order:

1. What is this thing and what is it not?
2. What must the reader have before starting?
3. What exact portal/editor path should they follow?
4. What files, identifiers, and limits matter?
5. How do they test a clean install and an upgrade?
6. What should they do when it fails?

Prefer tables for fields and decision points, short code blocks for exact values, and links to the next task. Avoid documenting private tokens, environment-specific paths, or UI behavior that is not present in the current app.

## Images

Put images in an `images/<page-slug>/` folder inside the same registered section and reference them with a relative path:

```markdown
![Server status page](images/setting-up-a-server/06-server-status-page.png)
```
