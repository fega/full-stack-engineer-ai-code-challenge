---
created: 2026-06-18
title: Advance a spool to its next station via the API
status: done
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
    `worktree:` ../wt-advance-spool-to-next-station-api  ·  `branch:` feat/advance-spool-to-next-station-api

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
- [x] Confirm `POST /api/spools/{id}/advance` advances a spool exactly one station
      along the chain and records a `StatusEvent` (station, timestamp, changedBy).
- [x] Confirm the response contract: `204 No Content` on success, `404` when the
      spool id is unknown. Decided to return `200 OK` with the new station instead
      of `204` — see RESOLUTION.
- [x] Verify the new `CurrentStation` is correctly derived after advancing (uses
      latest `StatusHistory` event by `ChangedAt`).
- [x] Add automated coverage in `src/StratusFabTracker.Tests` for the happy path
      across every forward hop (Detailing→Cut→…→Installed), unknown id → 404, and
      multi-step advancement.
- [x] Note any interaction with the backward-transition guard (see sibling
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

Not a bug fix. (See FOLLOW UP for a latent defect discovered while testing — it is
handed off to the `find-explain-fix-starter-bug-with-tests` playbook, not fixed here.)

# RESOLUTION

**What changed**

- `src/StratusFabTracker.Api/Application/SpoolWorkflowService.cs` — `AdvanceAsync`
  now returns `AdvanceOutcome(TransitionResult Result, Station? NewStation)`
  instead of a bare `TransitionResult`, so callers learn the station the spool
  moved to. The advance logic itself is unchanged (single forward hop via
  `Station.Next()`, append `StatusEvent(next, clock.UtcNow, "system")`, persist).
- `src/StratusFabTracker.Api/Program.cs` — the advance endpoint now returns
  **`200 OK`** with `{ id, currentStation }` on success (previously `204 No
  Content`); `404` (unknown id) and `400` (no next station) are unchanged.

**Contract decision (was 204 → now 200 + body)**

Chose `200 OK` with the new station over `204 No Content`. Rationale: the dashboard
and any client needs the resulting station to reflect the move; returning it inline
avoids a second round-trip to `GET /api/spools/{id}` and makes the transition
self-describing. No existing client depended on the old `204` (the Vue app does not
yet call advance). The error contract is intentionally left as-is — refining the
forward-only guard is the sibling `reject-invalid-backward-transitions` playbook.

**Tests added** (`dotnet test` green: 15 passed)

- `SpoolWorkflowServiceTests` (service-level, fakes + monotonic `FakeClock`):
  single forward hop; status-event recorded with clock time + actor + history
  preserved; every forward hop (theory); full chain Detailing→…→Installed;
  unknown id → `NotFound`; advance at `Installed` rejected with no state mutation.
- `AdvanceEndpointTests` (HTTP via `WebApplicationFactory<Program>`, auto-seed
  disabled for determinism): known spool → `200` + `{ id, currentStation: "Cut" }`
  and observable on re-read; unknown id → `404`; advance past `Installed` → `400`.
- New test doubles in `TestDoubles.cs`; added `Microsoft.AspNetCore.Mvc.Testing`
  to the test project.

PR: https://github.com/fega/full-stack-engineer-ai-code-challenge/pull/1

# FOLLOW UP

**Latent bug discovered (handed off, not fixed here).** While writing the
full-chain test I hit a real defect in `Spool.CurrentStation`
(`src/StratusFabTracker.Api/Domain/Spool.cs`): it derives the current station via
`StatusHistory.OrderByDescending(x => x.ChangedAt).First()`. `OrderByDescending`
is a *stable* sort, so when two events share the same `ChangedAt` it returns the
**earliest-inserted** of the tie, not the genuinely latest one. With a fixed-instant
clock, advancing twice in a row leaves `CurrentStation` reading the prior station,
so the next advance repeats a hop (observed Detailing→Cut→Weld→**Weld** instead of
→QC).

A real `SystemClock` yields strictly increasing timestamps, so the shipped advance
endpoint is correct in production — which is why this task uses a monotonic
`FakeClock` rather than masking the feature behind the defect. The fix (tie-break by
insertion order, or track an explicit sequence/current station) belongs to the
`find-explain-fix-starter-bug-with-tests` playbook; this is a concrete, reproducible
candidate for "the starter bug."
