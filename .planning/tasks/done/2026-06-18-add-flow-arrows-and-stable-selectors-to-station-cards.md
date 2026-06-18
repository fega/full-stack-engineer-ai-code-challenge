---
created: 2026-06-18
title: Add flow arrows and stable selectors to station cards
status: done
area: ui
---

# Task: Add flow arrows and stable selectors to station cards

# ON START

- Before doing any analysis or edits:
  - Set `status: in-progress` in this playbook's frontmatter.
  - Move this playbook from `.planning/tasks/todo/` to
    `.planning/tasks/in-progress/`, keeping the same filename.
- Treat that move as mandatory task-state bookkeeping: if you have started the
  task, it must no longer stay in `todo/`.
- **Work in an isolated git worktree on a dedicated branch — do not commit on the
  default branch.**
  - Create one named after this task, e.g.:
    ```sh
    git worktree add ../wt-station-card-arrows -b feat/station-card-arrows
    cd ../wt-station-card-arrows
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ../wt-add-flow-arrows-and-stable-selectors-to-station-cards  ·  `branch:` feat/add-flow-arrows-and-stable-selectors-to-station-cards

## Context

Vue 3 frontend in `src/stratus-fab-tracker-web/`. Station cards represent the
fabrication process stations (`STATION_ORDER` in `src/api.ts`: Detailing → Cut →
Weld → QC → Shipped → Installed). They are laid out in a wrapping CSS grid, which
visually reads left-to-right but does not communicate that the process is a
strictly **one-way** flow.

Two pages render station cards, both reusing the shared `.tile` styles from
`src/style.css` (plain global CSS; grid container `.grid` at style.css:80-84,
`.tile` styles at style.css:87-108):

- **Root / home page** — `src/views/SummaryView.vue:62-72`. Cards are
  `<RouterLink>` elements, one per in-flight station (terminal "Installed"
  station filtered out, SummaryView.vue:17).
- **WIP page** — `src/views/WipDetailView.vue:75-88`. Cards are `<button>`
  elements, all 6 stations, with `is-active` / `is-terminal` state classes.

Currently the cards carry only generic classes (`.tile`, `.tile-count`,
`.tile-label`, `.tile-tag`) and no stable ids, making them hard to target in
tests or future work.

## Task

- [x] Add a directional arrow (→) between consecutive station cards on **both**
      pages to indicate the one-way flow through the process.
  - Used explicit `<span class="station-flow-arrow" aria-hidden="true">→</span>`
    elements rendered between cards (v-for with index check, no trailing arrow
    after the last card). Container switched from `.grid` to a dedicated flex
    `.station-flow` so arrows sit between cards and wrap with them.
  - Visually consistent across SummaryView and WipDetailView.
- [x] Add clearer, stable class names and ids to the station card elements for
      future targeting: added a `station-card` class alongside `.tile`, a
      `data-station="<Name>"` attribute, and an `id="station-card-<slug>"`
      (e.g. `station-card-weld`) on both pages.
  - Applied consistently on both pages; existing `.tile*` selectors kept intact.
- [x] Verify the home page (5 cards, terminal excluded) and WIP page (6 cards)
      both render arrows correctly and that wrapping rows still look sensible.
  - `npm run build` passes; rendered a headless-Chrome preview of both layouts
    using the real stylesheet — arrows, active state, and terminal "Complete"
    badge all render correctly.

## Expected output

- Both the root page and WIP page show one-way `→` arrows between station cards.
- Station card elements carry a `station-card` class and a stable per-station
  id / data attribute on both pages.
- Existing behavior (RouterLink navigation on home, button select + active/
  terminal states on WIP) is unchanged.
- Frontend builds/lints cleanly; the change is visually verified in the running
  app.

# How to access

- Frontend app: `src/stratus-fab-tracker-web/` (Vue 3 + Vite + TypeScript).
- Key files:
  - `src/views/SummaryView.vue` (root page cards, lines ~62-72)
  - `src/views/WipDetailView.vue` (WIP page cards, lines ~75-88)
  - `src/style.css` (`.grid` ~80-84, `.tile` and friends ~87-108)
  - `src/api.ts` (`STATION_ORDER`, terminal station)
- Run locally with the project's standard dev command (`npm run dev` / `pnpm dev`
  inside `src/stratus-fab-tracker-web/`) to visually verify.

# ON FINISH

- When finished:
  - Fill ROOT CAUSE and RESOLUTION below.
  - Set `status: done` in this playbook's frontmatter.
  - Move this file to `.planning/tasks/done/` from `.planning/tasks/in-progress/`,
    renaming if needed to `YYYY-MM-DD-meaningful-name.md`.
  - Continue adding follow-up Q&A at the bottom.
  - Redact any sensitive data from the playbook.
- Commit the change on the task branch, then **open a PR** for review — do not
  merge straight to the default branch:
  ```sh
  git push -u origin feat/station-card-arrows
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-station-card-arrows
  ```

# ROOT CAUSE

N/A — this is a UI enhancement, not a bug fix.

# RESOLUTION

Added a one-way flow indicator and stable selectors to the station cards.

Files changed (in `src/stratus-fab-tracker-web/`):
- `src/style.css` — new `.station-flow` (flex-wrap row) and `.station-flow-arrow`
  rules. Station cards are styled via `.station-flow .station-card` (flex
  `1 1 150px`); existing `.tile*` rules untouched.
- `src/views/SummaryView.vue` — home page: container `.grid` → `.station-flow`;
  cards wrapped in a `<template v-for>` that emits a `→` arrow between cards
  (none after the last). Each card gains `station-card` class, `data-station`,
  and `id="station-card-<slug>"` via a `stationId()` helper.
- `src/views/WipDetailView.vue` — WIP page: same treatment for the button-based
  cards; `is-active` / `is-terminal` states and the "Complete" tag preserved.

Approach notes:
- Chose explicit arrow DOM elements over a CSS pseudo-element connector so the
  arrows wrap naturally with the flex row and are easy to target/verify.
- Arrows are `aria-hidden` (decorative); direction is conveyed visually.
- Known minor cosmetic: at a row-wrap boundary the last card of a row shows a
  trailing arrow. Wrapping itself is pre-existing behavior from the old
  auto-fill grid; direction still reads clearly. Left as-is for simplicity.

Verification: `npm run build` passes; headless-Chrome preview of both layouts
(home = 5 cards, WIP = 6 cards incl. terminal) rendered correctly.

PR: https://github.com/fega/full-stack-engineer-ai-code-challenge/pull/4

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
