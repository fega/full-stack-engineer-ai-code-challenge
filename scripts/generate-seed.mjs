#!/usr/bin/env node
/**
 * Deterministic seed data generator for the Stratus Fab Tracker.
 * Writes src/Data/spools.seed.json with ~200 spools.
 *
 * Usage: node scripts/generate-seed.mjs
 */

import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const SPOOL_COUNT = 200;
const PACKAGES = ['pkg-a', 'pkg-b', 'pkg-c', 'pkg-d', 'pkg-e', 'pkg-f'];
const PARTS = ['90ELBOW', 'TEE', 'COUPLING', 'FLANGE', 'VALVE', 'REDUCER', 'CAP', 'UNION'];
const STATIONS = ['Detailing', 'Cut', 'Weld', 'QC', 'Shipped', 'Installed'];

// Fixed reference date for reproducible output (matches interview kit era).
const TODAY = new Date('2026-06-05T12:00:00Z');

function mulberry32(seed) {
  return function () {
    let t = (seed += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const rand = mulberry32(42);

function pick(arr) {
  return arr[Math.floor(rand() * arr.length)];
}

function dateOnly(d) {
  return d.toISOString().slice(0, 10);
}

function addDays(d, days) {
  const copy = new Date(d);
  copy.setUTCDate(copy.getUTCDate() + days);
  return copy;
}

function buildStatusHistory(targetStationIndex) {
  const history = [];
  let cursor = addDays(TODAY, -30 - Math.floor(rand() * 20));

  for (let station = 0; station <= targetStationIndex; station++) {
    cursor = addDays(cursor, 1 + Math.floor(rand() * 2));
    history.push({
      station,
      changedAt: cursor.toISOString(),
      changedBy: 'seed',
    });
  }

  return history;
}

const spools = [];

for (let i = 1; i <= SPOOL_COUNT; i++) {
  const packageId = PACKAGES[(i - 1) % PACKAGES.length];
  const packageLetter = packageId.split('-')[1].toUpperCase();
  const spoolNumber = `${packageLetter}-${String(i).padStart(3, '0')}`;

  // Weight stations so WIP, past-due, and throughput all have signal.
  const stationRoll = rand();
  let targetStation;
  if (stationRoll < 0.12) targetStation = 0;
  else if (stationRoll < 0.28) targetStation = 1;
  else if (stationRoll < 0.42) targetStation = 2;
  else if (stationRoll < 0.55) targetStation = 3;
  else if (stationRoll < 0.68) targetStation = 4;
  else targetStation = 5;

  const dueOffset = targetStation === 5
    ? -5 - Math.floor(rand() * 25)
    : -10 + Math.floor(rand() * 35);
  const dueDate = dateOnly(addDays(TODAY, dueOffset));

  const bomCount = 1 + Math.floor(rand() * 3);
  const bom = Array.from({ length: bomCount }, () => ({
    partNumber: pick(PARTS),
    quantity: 1 + Math.floor(rand() * 12),
  }));

  let statusHistory = buildStatusHistory(targetStation);

  // Spread Installed events across the last 14 days for throughput.
  if (targetStation === 5) {
    const installedDayOffset = -Math.floor(rand() * 14);
    const installedAt = addDays(TODAY, installedDayOffset);
    installedAt.setUTCHours(12, 0, 0, 0);
    statusHistory = statusHistory.slice(0, -1);
    statusHistory.push({
      station: 5,
      changedAt: installedAt.toISOString(),
      changedBy: 'seed',
    });
  }

  spools.push({
    id: `spool-${String(i).padStart(3, '0')}`,
    packageId,
    spoolNumber,
    dueDate,
    bom,
    statusHistory,
  });
}

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const outPath = join(repoRoot, 'src', 'Data', 'spools.seed.json');
writeFileSync(outPath, `${JSON.stringify(spools, null, 2)}\n`, 'utf8');

const installed = spools.filter((s) => s.statusHistory.at(-1).station === 5).length;
const pastDue = spools.filter((s) => {
  const current = s.statusHistory.at(-1).station;
  return s.dueDate < dateOnly(TODAY) && current !== 5;
}).length;

console.log(`Wrote ${spools.length} spools to ${outPath}`);
console.log(`  Installed: ${installed}`);
console.log(`  Past due (not installed): ${pastDue}`);
console.log(`  Stations: ${STATIONS.join(' → ')}`);
