---
created: 2026-06-18
title: Dashboard shows spools past due
status: todo
area: ui
---

# Task: Dashboard shows spools past due

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
    git worktree add ../wt-past-due -b feat/past-due
    cd ../wt-past-due
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ____  ·  `branch:` ____

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
Past-due = spools whose `DueDate` is before today and that are not yet complete.
Starter (inherited) implementation:
- API: `DashboardService.GetDashboardAsync` computes `PastDueCount` as
  `DueDate < today && CurrentStation != Installed`
  (`src/StratusFabTracker.Api/Application/DashboardService.cs`). Returned in
  `DashboardDto.PastDueCount`.
- UI: `src/stratus-fab-tracker-web/src/App.vue` shows `Past due: {{ pastDueCount }}`.

Judgment calls to resolve and document:
- "Complete" cutoff: is a `Shipped`-but-not-`Installed` spool past due? Current
  code only excludes `Installed`.
- `today` uses `DateTime.UtcNow` directly in this service (the workflow/throughput
  services use the injected `IClock`); consider routing through `IClock` for
  testability/consistency.
- Whether to expose the past-due **list** (ids/spool numbers) for actionability,
  not just a count.

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Confirm and document the past-due definition (date comparison + which
      stations count as "still open").
- [ ] Consider routing `now` through the injected `IClock` for consistency and
      testability.
- [ ] Decide whether to surface a past-due list (with due date + station) in
      addition to the count; implement if in scope.
- [ ] Add backend test(s) in `src/StratusFabTracker.Tests` covering: due in past +
      open → counted; due in past + Installed → not counted; due today/future →
      not counted (boundary case).
- [ ] Ensure the UI clearly highlights past-due (e.g. emphasis when count > 0).

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- `GET /api/dashboard` returns an accurate past-due count (and list, if added).
- Boundary behavior (due == today) is defined and tested.
- The dashboard surfaces past-due spools clearly; `dotnet test` passes.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- API: `cd src/StratusFabTracker.Api && dotnet run` (`http://localhost:5072`).
- Web: `cd src/stratus-fab-tracker-web && npm run dev`.
- Tests: `dotnet test` from repo root. Seed data: `src/Data/spools.seed.json`.

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
  git push -u origin feat/past-due
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-past-due
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
