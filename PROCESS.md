# Process

Use this file to summarize your approach after you work.

Keep it short and concrete:
- what AI tools you used
- where AI output was wrong or misleading
- how you validated the code you kept
- your assumptions for throughput
- what you'd do next with more time

## Throughput model (assumptions)

Computed in `ThroughputService.GetThroughputAsync`
(`src/StratusFabTracker.Api/Application/ThroughputService.cs`), exposed at
`GET /api/throughput`, rendered in the Overview "Throughput" card.

- **Window** — the trailing `WindowDays = 14` calendar days, **inclusive of today**:
  `[today - 13, today]`. "Today" is the UTC date from the injected `IClock`
  (`SystemClock` in production, `FakeClock` in tests), so the math is deterministic
  under test.
- **Completion** — a spool **first** reaching the terminal `Installed` station.
  Each spool counts **at most once**, on the UTC calendar day of its earliest
  `Installed` event. (The starter counted every `Installed` event; deduping per
  spool means a stray repeat event can't inflate the number. With the seed data,
  which has one `Installed` event per spool, the two approaches agree.)
- **Demand ("due")** — spools whose `DueDate` falls on a given day in the window.
- **Daily series** — exactly one row per day across the whole window, **including
  zero days** for both completions and due dates, so the series is gap-free.
- **Rates** — `CompletedPerDay` and `DuePerDay` are per-day means over the window
  (`total / WindowDays`, not `total / busy-days`).
- **Keeping up** — `CompletedPerDay >= DuePerDay`.

### Is "keeping up" a sound comparison?

Yes, dimensionally. Both sides are **spools/day measured over the same 14-day
window**, so this is *not* a unit mismatch — and since both rates share the `/14`
denominator, the test reduces to *"did we complete at least as many spools as came
due in the window?"*. I kept the comparison and clarified the code/comments rather
than changing the formula.

It is, however, a **coarse** signal by design:

- It ignores **backlog carried in** from before the window (a spool due last month
  and still unbuilt never enters `DuePerDay`).
- It does not require that the spools **completed** in the window are the **same
  ones** that came **due** in it — it compares aggregate flow in vs. flow out.
- The final day is a **partial day** (only the elapsed part of "today"), which can
  slightly understate `CompletedPerDay`.

So it answers *"are we finishing work at least as fast as new work is coming due, on
average?"* — not *"is every individual spool on time?"* (the dashboard's "Past due"
count covers the latter).

### What I'd do next with more time

- Track **per-spool on-time completion** (completed on/before its own due date) for a
  truer SLA view, alongside the aggregate rate.
- Carry **open backlog** into the demand figure so a growing queue is visible.
- Make the window length a query parameter (e.g. 7 / 14 / 30 days) instead of a const.

## Validation

- `dotnet test` — 28 passing, including 7 new deterministic
  `ThroughputServiceTests` (fixed clock at 2026-06-18) pinning daily buckets, the
  full-window/zero-day coverage, first-`Installed`-only dedup, window boundaries, the
  per-day averages, and both `KeepingUp` branches.
- `npm run build` (vite) — the web app compiles with the new per-day table.
