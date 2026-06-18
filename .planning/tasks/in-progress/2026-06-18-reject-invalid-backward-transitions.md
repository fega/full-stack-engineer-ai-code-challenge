---
created: 2026-06-18
title: Reject invalid backward / out-of-range station transitions
status: in-progress
area: api
---

# Task: Reject invalid backward / out-of-range station transitions

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
    git worktree add ../wt-reject-backward -b feat/reject-backward
    cd ../wt-reject-backward
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ../wt-reject-backward  ·  `branch:` feat/reject-backward

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
Spools must only move forward along `Detailing → Cut → Weld → QC → Shipped →
Installed`. Backward moves and advancing past `Installed` must be rejected.

Current starter behavior (inherited code):
- `SpoolWorkflowService.AdvanceAsync` only ever advances via
  `CurrentStation.Next()`; when at `Installed`, `Next()` returns `null` and the
  service returns `TransitionResult.InvalidTransition`
  (`src/StratusFabTracker.Api/Application/SpoolWorkflowService.cs`).
- `Program.cs:31` maps `InvalidTransition → 400 BadRequest` with message
  "Spool cannot move backward or beyond Installed".
- `Station.Next()` defines the only legal forward edges
  (`src/StratusFabTracker.Api/Domain/Station.cs`).

Edge to scrutinize: the only mutation path is "advance one step," so a literal
backward move isn't currently expressible. This task is about making the rule
**explicit, tested, and robust** — and deciding whether the API should accept a
target station (which would make backward moves expressible and thus needing an
explicit guard).

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Decide the transition contract: advance-only (`Next()`) vs. a
      `move-to-{station}` API. Document the decision and rationale in RESOLUTION.
- [ ] Ensure advancing a spool already at `Installed` is rejected with a clear
      status code + message (currently `400 InvalidTransition`).
- [ ] If a target-station move is supported, add an explicit guard rejecting any
      target that is not exactly one step forward (no backward, no skipping).
- [ ] Add tests in `src/StratusFabTracker.Tests` for: advance-past-Installed
      rejected; (if applicable) backward target rejected; skip-ahead rejected;
      legal forward move accepted.
- [ ] Confirm rejected transitions do **not** mutate `StatusHistory`.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- `dotnet test` passes with tests asserting invalid transitions are rejected and
  leave state unchanged.
- The endpoint returns a documented, consistent error code for invalid
  transitions (e.g. `400` with a descriptive message).
- The legal-transition rule lives in one well-tested place (domain/service), not
  duplicated.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- Tests: `dotnet test` from repo root.
- Manual: advance a seeded spool to `Installed`, then `curl -i -X POST
  http://localhost:5072/api/spools/<id>/advance` and confirm the rejection.

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
  git push -u origin feat/reject-backward
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-reject-backward
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
