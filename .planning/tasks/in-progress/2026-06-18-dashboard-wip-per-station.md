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

your task is to implement a great UI for this feature, you will have a summary
view in the root path but clicking on a button will send you to the Detail WIP
per station page, where you can click any category and see the list of spools
that are in that state in a table

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
First pass (minimal — shipped in PR #1):
- [x] Verify `WipByStation` counts every spool exactly once and includes all six
      stations (including zero counts).
- [x] Ensure the UI displays stations in domain order, not arbitrary dict order.
- [x] Decide and document whether `Installed` (terminal) belongs in the WIP view
      or should be visually separated from in-flight stations.
- [x] Add backend test(s) in `src/StratusFabTracker.Tests` asserting WIP counts
      for a known seeded/constructed set of spools.
- [x] Light UI polish so counts are legible (labels + numbers); keep scope tight.

Second pass (expanded scope — full UI per the paragraph above):
- [ ] Add `GET /api/spools` returning per-spool summaries (number, package, due
      date, current station, past-due flag) so the detail table can be populated.
- [ ] Introduce client-side routing (`vue-router`): `/` summary + `/wip` detail.
- [ ] Summary view at `/` with a button that navigates to the WIP detail page.
- [ ] WIP detail page `/wip`: click any station category to see a table of the
      spools currently in that state.
- [ ] Polish the UI into a clean, legible dashboard.

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

Not a bug fix. The inherited code was functionally correct but relied on
`Dictionary<string,int>` serialization order to convey domain station order, which
is incidental rather than guaranteed by the contract, and it presented the terminal
`Installed` station inline with in-flight WIP.

# RESOLUTION

**Backend** (`src/StratusFabTracker.Api/Application/DashboardService.cs`):
- Replaced the `Dictionary<string,int> WipByStation` with an explicitly ordered
  `IReadOnlyList<WipStationCount>` (new record `WipStationCount(string Station, int Count)`),
  built via `Enum.GetValues<Station>().OrderBy(s => (int)s)`. Domain order
  (Detailing → Cut → Weld → QC → Shipped → Installed) is now part of the contract,
  not a serialization artifact.
- Counting is unchanged in spirit: every station is emitted including zero counts,
  and because each spool has exactly one `CurrentStation`, the counts partition the
  spool set and sum to the total.

**Installed decision**: `Installed` is the terminal station (`Station.Installed.Next()`
is null) — those spools are no longer work-in-progress. It is **kept in the API payload**
so the six per-station counts still sum to the spool total, but is **visually separated
in the UI**: the "WIP per station" table lists only in-flight stations (Detailing →
Shipped) plus an "In progress" subtotal, with Installed shown beneath as
"Installed (complete)".

**Frontend** (`src/stratus-fab-tracker-web/src/App.vue`):
- Updated the `DashboardDto` type to the array shape; added computed
  `inFlightStations`, `installedCount`, and `wipTotal`.
- Rendered WIP as a small table with right-aligned, tabular-figure counts for
  legibility; Installed and Past due shown separately below.

**Tests** (`src/StratusFabTracker.Tests/DashboardServiceTests.cs`): 3 new tests
pin (a) all six stations present in domain order, (b) each spool counted once with
zero-count stations present and counts summing to the total, (c) empty repository
yields six zero counts. `dotnet test` → 5 passed (2 pre-existing + 3 new).

**Verified**: `GET /api/dashboard` against seed data returns the ordered array with
counts 29/27/23/27/36/58 summing to 200 (total spools). `npm run build` succeeds.

PR: https://github.com/GTP-Services/full-stack-engineer-ai-code-challenge/pull/1


# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
