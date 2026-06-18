---
created: 2026-06-18
title: Add flow arrows and stable selectors to station cards
status: todo
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
    `worktree:` ____  ·  `branch:` ____

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

- [ ] Add a directional arrow (→) between consecutive station cards on **both**
      pages to indicate the one-way flow through the process.
  - Decide on the simplest robust approach for the wrapping grid (e.g. a CSS
    `::after`/`::before` connector, or explicit arrow elements between cards).
    Arrows should sit between cards, not after the last one.
  - Keep it visually consistent across SummaryView and WipDetailView.
- [ ] Add clearer, stable class names and ids to the station card elements for
      future targeting, e.g. a `station-card` class (in addition to / alongside
      the existing `.tile`) and a per-station id/data attribute derived from the
      station name (e.g. `data-station="weld"` or `id="station-card-weld"`).
  - Apply consistently on both pages.
  - Prefer adding new selectors over renaming existing ones unless the rename is
    clean and updated everywhere (style.css + both views).
- [ ] Verify the home page (5 cards, terminal excluded) and WIP page (6 cards)
      both render arrows correctly and that wrapping rows still look sensible.

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

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
