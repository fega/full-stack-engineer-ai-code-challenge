# Stratus Fab Tracker

## Interview documents

Read these before you start:

- [PRIVACY_RELEASE.md](PRIVACY_RELEASE.md) — sign before running `./start.sh`
- [INTERVIEW_RULES.md](INTERVIEW_RULES.md) — recording workflow, time-box, and setup notes
- [PROCESS.md](PROCESS.md) — short write-up template to complete after the session
- [RUBRIC_OVERVIEW.md](RUBRIC_OVERVIEW.md) — what the proctor observes

## What you're building

This starter is a small fabrication-tracker app for MEP shop workflows. It includes a .NET API, a Vue + TypeScript dashboard, and a seeded in-memory dataset of about 200 spools moving through shop stations.

Your job is to extend it so a user can advance a spool to its next station and see the dashboard reflect current work-in-progress, past-due spools, and a basic throughput view.

The domain is intentionally small:

- A **spool** moves through: `Detailing → Cut → Weld → QC → Shipped → Installed`
- A spool belongs to a **package**
- A spool has a **bill of materials** and a **due date**
- Status changes are tracked over time

## Time-box

**60 minutes**

Please keep your implementation tight and focused.

## What the starter provides

- **.NET API** with an in-memory repository (no database to install)
- **Vue + TypeScript** dashboard
- **~200 spools** in `src/Data/spools.seed.json`, loaded automatically when the API starts
- Existing dashboard and throughput endpoints
- Optional project hook configs for Claude Code, Cursor, and Codex that can append structured events to `interview.log`

> **Heads up:** parts of this starter were generated with AI and lightly reviewed. It runs, but it has rough edges — exactly the kind you'd inherit on a real team. Treat it as a codebase you've just been handed, not as a reference you should trust.

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 18+ (for the web app and optional hook scripts)

No database installation is required.

### 1) Install dependencies

From the repo root:

```bash
cd src/StratusFabTracker.Api
dotnet restore

cd ../stratus-fab-tracker-web
npm install
```

### 2) Run the API

```bash
cd src/StratusFabTracker.Api
dotnet run
```

The API auto-seeds from `src/Data/spools.seed.json` on startup. You do not need to run a seed script.

### 3) Run the web app

```bash
cd src/stratus-fab-tracker-web
npm run dev
```

The Vite dev server proxies `/api` to `http://localhost:5072`.

### 4) Run tests

From the repo root:

```bash
dotnet test
```

You are expected to add your own tests for the transition logic and throughput behavior as part of the exercise.

## Recording the session

1. Sign [PRIVACY_RELEASE.md](PRIVACY_RELEASE.md)
2. Run `./start.sh` from the repo root — this records your terminal session
3. Work in your normal AI-assisted workflow (Claude Code, Cursor, Codex, or any other tool)
4. When finished, exit the recording shell and run `./end.sh`

`start.sh` and `end.sh` depend only on the terminal recording. If your AI tool supports project hooks, the repo includes optional configs under `.claude/`, `.cursor/`, and `.codex/` that may append structured events to `interview.log`. Codex users may need to trust project hooks via `/hooks` before they run.

## Acceptance criteria

- A spool can be advanced to its next station through the API
- Invalid backward transitions are rejected
- The dashboard shows WIP per station
- The dashboard shows spools past due
- A throughput view is added with reasonable assumptions documented by you
- The starter bug is found, explained, fixed, and covered by tests

## Maintainer notes

To regenerate the seed dataset:

```bash
node scripts/generate-seed.mjs
```

This overwrites `src/Data/spools.seed.json` with a deterministic ~200-spool dataset.

## Process

See [INTERVIEW_RULES.md](INTERVIEW_RULES.md) for the recording and interview workflow.
