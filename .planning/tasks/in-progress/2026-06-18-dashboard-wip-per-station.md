---
created: 2026-06-18
title: Dashboard shows WIP per station
status: in-progress
area: ui
---

# Task: Dashboard shows WIP per station

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
    git worktree add ../wt-wip-per-station -b feat/wip-per-station
    cd ../wt-wip-per-station
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ../wt-dashboard-wip-per-station  ·  `branch:` feat/dashboard-wip-per-station

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
WIP (work in progress) per station = count of spools whose `CurrentStation` is
each station. The starter already wires this end to end (inherited code to verify
and polish):
- API: `DashboardService.GetDashboardAsync` builds `WipByStation` by counting
  spools per `Station` enum value; returned as `DashboardDto`
  (`src/StratusFabTracker.Api/Application/DashboardService.cs`), exposed at
  `GET /api/dashboard` (`Program.cs:23`).
- UI: `src/stratus-fab-tracker-web/src/App.vue` renders a "WIP per station" list
  from `dashboard.wipByStation`.

Things to scrutinize: station ordering in the UI (the dict may not preserve the
domain order `Detailing→…→Installed`), whether `Installed` should count as WIP or
be presented separately, and zero-count stations being shown.

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Verify `WipByStation` counts every spool exactly once and includes all six
      stations (including zero counts).
- [ ] Ensure the UI displays stations in domain order, not arbitrary dict order.
- [ ] Decide and document whether `Installed` (terminal) belongs in the WIP view
      or should be visually separated from in-flight stations.
- [ ] Add backend test(s) in `src/StratusFabTracker.Tests` asserting WIP counts
      for a known seeded/constructed set of spools.
- [ ] Light UI polish so counts are legible (labels + numbers); keep scope tight.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- `GET /api/dashboard` returns accurate per-station counts summing to the spool
  total.
- The dashboard renders WIP per station in chain order.
- A backend test pins the counting logic; `dotnet test` passes.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- API: `cd src/StratusFabTracker.Api && dotnet run` (`http://localhost:5072`).
- Web: `cd src/stratus-fab-tracker-web && npm install && npm run dev` (Vite proxies
  `/api` to the API).
- Tests: `dotnet test` from repo root.

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
  git push -u origin feat/wip-per-station
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-wip-per-station
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
