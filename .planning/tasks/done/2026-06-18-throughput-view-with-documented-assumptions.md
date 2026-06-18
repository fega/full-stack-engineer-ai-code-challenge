---
created: 2026-06-18
title: Throughput view with documented assumptions
status: done
area: fullstack
---

# Task: Throughput view with documented assumptions

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
    git worktree add ../wt-throughput -b feat/throughput
    cd ../wt-throughput
    ```
  - Make the playbook state-move commit (above) on this branch.
  - Keep the worktree path noted here so the next session can find it:
    `worktree:` ../wt-throughput-view-with-documented-assumptions  ·  `branch:` feat/throughput-view-with-documented-assumptions

## Context

<!-- Background the AI needs: which part of the codebase / service, what recently
     changed, why this matters. -->
A throughput view should show how fast spools complete and whether the shop is
keeping up with demand. **The acceptance criterion explicitly requires the
assumptions to be documented by you.**

Starter (inherited) implementation:
- API: `ThroughputService.GetThroughputAsync`
  (`src/StratusFabTracker.Api/Application/ThroughputService.cs`) computes, over a
  trailing 14-day window: daily completion counts (`StatusHistory` events where
  `Station == Installed`), `CompletedPerDay` (14-day average), `DuePerDay`
  (spools due in window / 14), and a `KeepingUp` boolean. Exposed at
  `GET /api/throughput` (`Program.cs:24`).
- UI: `src/stratus-fab-tracker-web/src/App.vue` renders completed/day, due/day,
  keeping-up, and a per-day list.

Assumptions baked into the starter that must be examined and written down:
- "Completion" = reaching `Installed` (vs. `Shipped`).
- 14-day trailing window; throughput = mean completions/day over that window.
- "Keeping up" = `CompletedPerDay >= DuePerDay`, comparing a completion **rate**
  against a **due rate** — verify this comparison is meaningful and not unit-mismatched.
- Uses injected `IClock` for "today".

## Task



<!-- What to do. Use sub-headings or a checklist for multi-step work. -->
- [x] show the list of due per day in the Throughput section, in a table
- [x] Define and **document** the throughput model: window length, what counts as
      completion, how the rate and "keeping up" are derived, and known limitations.
      Put the write-up in RESOLUTION and/or a short section in `README.md`/`PROCESS.md`.
- [x] Review the `KeepingUp` / `DuePerDay` logic for soundness; adjust if the
      comparison is misleading, and document the reasoning.
- [x] Confirm the daily series always covers the full window (including zero days).
- [x] Add backend test(s) in `src/StratusFabTracker.Tests` using a fixed `IClock`
      and constructed history to pin daily buckets, the average, and `KeepingUp`.
- [x] Make the UI throughput view readable (e.g. a simple trend list/sparkline);
      keep scope tight within the time-box.

## Expected output

<!-- What "done" looks like: response shape, file moved, endpoint verified, tests
     passing, etc. -->
- `GET /api/throughput` returns a documented, defensible throughput model.
- Assumptions are written down where a reviewer can find them.
- Deterministic tests (fixed clock) pin the math; `dotnet test` passes.
- The dashboard presents throughput legibly.

# How to access

<!-- Any infra / credentials / commands the AI needs to run or inspect the system. -->
- API: `cd src/StratusFabTracker.Api && dotnet run` (`http://localhost:5072`).
- Web: `cd src/stratus-fab-tracker-web && npm run dev`.
- Tests: `dotnet test` from repo root. `IClock`/`SystemClock` live in the API
  Application/Infrastructure layers (DI registered in `Program.cs`).

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
  git push -u origin feat/throughput
  gh pr create --fill --base main
  ```
  - Record the PR URL in RESOLUTION below.
- Once the PR is merged, clean up the worktree:
  ```sh
  git worktree remove ../wt-throughput
  ```

# ROOT CAUSE

Not a bug fix. The starter throughput view was under-specified and under-documented:
its assumptions were implicit, it exposed only aggregate rates (no per-day demand),
and the `KeepingUp`/`DuePerDay` comparison was flagged for a possible unit mismatch.

# RESOLUTION

## What changed

- **`ThroughputService.cs`** — kept the trailing-window model but made it explicit
  and defensible:
  - Introduced `public const int WindowDays = 14`; window is `[today - 13, today]`,
    inclusive of today, with `today` from the injected `IClock`.
  - A **completion** is now a spool's **first** `Installed` event (deduped per spool),
    bucketed by UTC calendar day — a stray repeat `Installed` can no longer inflate
    the count.
  - Added a **per-day "due" series**: `ThroughputDayDto` is now
    `(Day, Completed, Due)`, populated from each spool's `DueDate`.
  - The daily series still emits **one row per day across the whole window, including
    zero days** for both completions and due dates.
  - `DuePerDay` is now the per-day mean of the same padded series
    (`daily.Average(x => x.Due)`), consistent with `CompletedPerDay`.
  - Documented the model and the keeping-up reasoning in XML doc comments.
- **Web** — `api.ts` `ThroughputDayDto` gains `due: number`; `SummaryView.vue`
  renders a **per-day table** (Day / Completed / Due) with a small inline bar
  sparkline on the Completed column and a totals footer.
- **Tests** — new `ThroughputServiceTests.cs` (7 tests, fixed `FakeClock` at
  2026-06-18): full-window/zero-day coverage, completion bucketing + window average,
  first-`Installed`-only dedup, inclusive window boundaries, per-day due buckets +
  average, and both `KeepingUp` branches. `dotnet test` → **28 passing**.
- **`PROCESS.md`** — added a "Throughput model (assumptions)" section (the
  authoritative write-up a reviewer should read).

## Is `KeepingUp` sound? (the flagged concern)

**Yes — not a unit mismatch.** `CompletedPerDay` and `DuePerDay` are both
*spools/day over the same 14-day window*; since both carry the same `/14`
denominator, `CompletedPerDay >= DuePerDay` reduces to *"did we complete at least as
many spools as came due in the window?"*. I therefore kept the formula and clarified
the code/comments rather than changing it.

It is intentionally **coarse**: it ignores backlog carried in from before the window,
does not require the completed spools to be the same ones that came due, and the last
day is partial. It answers *"are we finishing at least as fast as work comes due, on
average?"* — not per-spool on-time status (that's the dashboard's "Past due" count).
Full reasoning + "what I'd do next" in `PROCESS.md`.

## Verification

- `GET /api/throughput` confirmed live (port 5099) returning the padded daily series
  with the new `due` field per day; window `[2026-06-05, today]`.
- `dotnet test` → 28 passing. `npm run build` (vite) → clean.

PR: <!-- filled after push -->


# FOLLOW UP

[Any extra question, request, or conversation regarding this playbook with the AI
should be placed here]
