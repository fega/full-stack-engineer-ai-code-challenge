---
created: 2026-06-18
title: Move the /wip table to an accordion on the root path
status: in-progress
area: ui
---

# Task: Move the /wip table to an accordion on the root path

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
    git worktree add ../wt-wip-accordion -b feat/wip-accordion
    cd ../wt-wip-accordion
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ../wt-wip-accordion  ·  `branch:` feat/wip-accordion

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
Vue 3 + vue-router frontend lives in `src/stratus-fab-tracker-web/`. Two routes
(`src/router.ts`):

- `/`     → `src/views/SummaryView.vue` — dashboard: WIP stats, a station-flow
  row of tiles (each `RouterLink` to `/wip?station=…`), and a 14-day throughput
  card.
- `/wip`  → `src/views/WipDetailView.vue` — same station-flow row (as `<button>`s
  that drive `?station=`) plus a **table** of the spools at the selected station
  (columns: Spool, Package, Due date, Status). The selected station is held in
  the URL query so the view is linkable and back-button friendly.

The user wants the spool table currently on `/wip` surfaced directly on the root
`/` page inside an accordion, instead of (or in addition to) requiring a
navigation to `/wip`.

Decide during ON START whether `/wip` should remain as a standalone route or be
fully folded into the root accordion — note the decision in RESOLUTION.

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Add an accordion to `SummaryView.vue` (root `/`) that contains the spool
      table currently rendered in `WipDetailView.vue`.
- [ ] Reuse the existing station selection model: clicking a station tile should
      expand/select that station's spools. Keep selection in the URL query
      (`?station=`) so the existing linkable/back-button behavior is preserved.
- [ ] Move the table markup, `spoolsAtSelected` filtering, and `formatDue`
      date helper from `WipDetailView.vue` into the root view (or a shared
      component) so logic isn't duplicated.
- [ ] Preserve the `stationCardId` / `data-station` stable selectors and the
      past-due / on-track badge rendering.
- [ ] Decide the fate of the `/wip` route and `WipDetailView.vue` (keep, redirect
      to `/`, or remove) and update `router.ts` accordingly.
- [ ] Style the accordion consistently with the existing `.card` / `.tile`
      design language in `style.css`.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- On the root path `/`, the per-station spool table is reachable via an accordion
  without navigating to `/wip`.
- Selecting a station expands its spool list; selection still reflected in the URL.
- No duplicated table/format logic between views; stable `data-station` selectors
  retained.
- App builds and runs cleanly (`npm run build` / dev server) with no console
  errors.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- Frontend: `src/stratus-fab-tracker-web/` — `npm install`, `npm run dev`,
  `npm run build`.
- Relevant files: `src/router.ts`, `src/views/SummaryView.vue`,
  `src/views/WipDetailView.vue`, `src/api.ts`, `src/style.css`.

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
  git push -u origin feat/wip-accordion
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-wip-accordion
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
