---
created: 2026-06-18
title: Advance a spool to its next station via the API
status: todo
area: api
---

# Task: Advance a spool to its next station via the API

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
    git worktree add ../wt-advance-spool -b feat/advance-spool
    cd ../wt-advance-spool
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ____  ·  `branch:` ____

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
Stratus Fab Tracker: a .NET 8 minimal API + Vue/TS dashboard. Spools move through
the station chain `Detailing → Cut → Weld → QC → Shipped → Installed`.

A starter implementation of this endpoint **already exists** — treat it as code
you've inherited and need to confirm/solidify, not build from scratch:
- Endpoint: `POST /api/spools/{id}/advance` in
  `src/StratusFabTracker.Api/Program.cs:31`.
- Logic: `SpoolWorkflowService.AdvanceAsync` in
  `src/StratusFabTracker.Api/Application/SpoolWorkflowService.cs` — looks up the
  spool, computes `CurrentStation.Next()`, appends a `StatusEvent`, persists.
- Station order + `Next()`: `src/StratusFabTracker.Api/Domain/Station.cs`.
- `CurrentStation` is derived from `StatusHistory` (latest event) in
  `src/StratusFabTracker.Api/Domain/Spool.cs`.
- Storage: `InMemorySpoolRepository` (singleton, seeded from
  `src/Data/spools.seed.json`).

This is the foundational acceptance criterion the other API work builds on.

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Confirm `POST /api/spools/{id}/advance` advances a spool exactly one station
      along the chain and records a `StatusEvent` (station, timestamp, changedBy).
- [ ] Confirm the response contract: `204 No Content` on success, `404` when the
      spool id is unknown. Decide whether success should instead return the updated
      spool / new station (document the decision in RESOLUTION).
- [ ] Verify the new `CurrentStation` is correctly derived after advancing (uses
      latest `StatusHistory` event by `ChangedAt`).
- [ ] Add automated coverage in `src/StratusFabTracker.Tests` for the happy path
      across every forward hop (Detailing→Cut→…→Installed), unknown id → 404, and
      multi-step advancement.
- [ ] Note any interaction with the backward-transition guard (see sibling
      playbook `reject-invalid-backward-transitions`) but keep that scope there.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- `dotnet test` passes with new tests covering forward advancement and 404.
- Advancing a seeded spool via the API moves it one station forward and is visible
  in the dashboard WIP counts.
- The chosen success response shape is documented in RESOLUTION.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- Run API: `cd src/StratusFabTracker.Api && dotnet run` (listens on
  `http://localhost:5072`).
- Tests: `dotnet test` from repo root.
- Manual check: `curl -i -X POST http://localhost:5072/api/spools/<id>/advance`
  (grab an id from `GET /api/dashboard` flow or `src/Data/spools.seed.json`).

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
  git push -u origin feat/advance-spool
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-advance-spool
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
