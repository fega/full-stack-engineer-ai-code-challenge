# Stratus Fab Tracker

## What you're building

This starter is a small fabrication-tracker app for MEP shop workflows. It includes a .NET API, a Vue + TypeScript dashboard, and a seeded dataset of spools moving through shop stations.

Your job is to extend it so a user can advance a spool to its next station and see the dashboard reflect current work-in-progress, past-due spools, and a basic throughput view.

The domain is intentionally small:

- A **spool** moves through: `Detailing → Cut → Weld → QC → Shipped → Installed`
- A spool belongs to a **package**
- A spool has a **bill of materials** and a **due date**
- Status changes are tracked over time

## Time-box

**60 minutes**

Please keep your implementation tight and focused.

## Getting started

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

### 3) Run the web app

```bash
cd src/stratus-fab-tracker-web
npm run dev
```

### 4) Write and run your own tests

You are expected to add your own tests for the transition logic and throughput behavior as part of the exercise.

## Acceptance criteria

- A spool can be advanced to its next station through the API
- Invalid backward transitions are rejected
- The dashboard shows WIP per station
- The dashboard shows spools past due
- A throughput view is added with reasonable assumptions documented by you
- The starter bug is found, explained, fixed, and covered by tests

## Process

See `INTERVIEW_RULES.md` for the recording and interview workflow.
