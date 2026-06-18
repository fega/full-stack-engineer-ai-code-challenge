---
created: 2026-06-18
title: Find, explain, fix, and test the starter bug
status: todo
area: testing
---

# Task: Find, explain, fix, and test the starter bug

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
    git worktree add ../wt-starter-bug -b fix/starter-bug
    cd ../wt-starter-bug
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ____  ·  `branch:` ____

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
The README warns the starter "runs, but it has rough edges — exactly the kind
you'd inherit on a real team... Treat it as a codebase you've just been handed,
not as a reference you should trust." The acceptance criteria require that **the
starter bug is found, explained, fixed, and covered by tests.**

This is a debugging task: reproduce first, write a failing test that captures the
defect, then fix. Do not assume the bug — confirm it with a repro.

Candidate areas to investigate (hints, not conclusions — verify each):
- `Spool.CurrentStation` derives "current" from `StatusHistory` via
  `OrderByDescending(x => x.ChangedAt).First()` — behavior on tied timestamps or
  out-of-order seed history (`src/StratusFabTracker.Api/Domain/Spool.cs`).
- `DashboardService` uses `DateTime.UtcNow` directly while other services use the
  injected `IClock` — inconsistency and possible date/boundary bug
  (`src/StratusFabTracker.Api/Application/DashboardService.cs`).
- `ThroughputService` `KeepingUp` / `DuePerDay` — rate vs. count comparison,
  window edges, average over zero-padded days
  (`src/StratusFabTracker.Api/Application/ThroughputService.cs`).
- Past-due definition excludes only `Installed` (not `Shipped`) — possible
  off-by-one / semantics bug.
- `InMemorySpoolRepository` returns live references; mutation aliasing between
  `GetByIdAsync`/`GetAllAsync`/`UpdateAsync`.
- Seed loading (`SeedData` + `src/Data/spools.seed.json`) and date parsing.

## Task

<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [ ] Reproduce a concrete, incorrect behavior and capture exactly how it
      manifests (inputs → wrong output).
- [ ] Write a **failing** test in `src/StratusFabTracker.Tests` that demonstrates
      the bug before any fix.
- [ ] Identify and document the root cause (fill ROOT CAUSE below).
- [ ] Apply the minimal correct fix.
- [ ] Confirm the new test now passes and no existing tests regress.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- A clear written explanation of the bug (symptom + root cause) in ROOT CAUSE.
- A regression test that fails on the old code and passes on the fixed code.
- `dotnet test` green; fix is minimal and scoped.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- Tests: `dotnet test` from repo root (existing tests in
  `src/StratusFabTracker.Tests/StationTests.cs`).
- API: `cd src/StratusFabTracker.Api && dotnet run` for manual repro.
- Seed: `src/Data/spools.seed.json` (regenerate via `node scripts/generate-seed.mjs`).

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
  git push -u origin fix/starter-bug
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-starter-bug
  ```

# ROOT CAUSE

[if task is fixing a bug, report the root cause here]

# RESOLUTION

[place here the resolution of the ticket]

# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
