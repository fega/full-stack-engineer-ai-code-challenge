<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';

type WipStationCount = { station: string; count: number };
type DashboardDto = {
  wipByStation: WipStationCount[];
  pastDueCount: number;
};

type ThroughputDayDto = { day: string; completed: number };
type ThroughputDto = {
  daily: ThroughputDayDto[];
  completedPerDay: number;
  duePerDay: number;
  keepingUp: boolean;
};

const dashboard = ref<DashboardDto | null>(null);
const throughput = ref<ThroughputDto | null>(null);
const error = ref('');

// Installed is the terminal station: those spools are no longer work-in-progress,
// so they are shown separately from the in-flight stations (Detailing -> Shipped).
const inFlightStations = computed(() =>
  dashboard.value?.wipByStation.filter(s => s.station !== 'Installed') ?? []
);
const installedCount = computed(() =>
  dashboard.value?.wipByStation.find(s => s.station === 'Installed')?.count ?? 0
);
const wipTotal = computed(() =>
  inFlightStations.value.reduce((sum, s) => sum + s.count, 0)
);

async function load() {
  try {
    const [d, t] = await Promise.all([
      fetch('/api/dashboard').then(r => r.json()),
      fetch('/api/throughput').then(r => r.json())
    ]);
    dashboard.value = d;
    throughput.value = t;
  } catch {
    error.value = 'Unable to load dashboard';
  }
}

onMounted(load);
</script>

<template>
  <main style="font-family: sans-serif; padding: 24px; max-width: 960px; margin: 0 auto;">
    <h1>Stratus Fab Tracker</h1>
    <p v-if="error">{{ error }}</p>

    <section v-if="dashboard">
      <h2>WIP per station</h2>
      <table style="border-collapse: collapse; min-width: 280px;">
        <tbody>
          <tr v-for="s in inFlightStations" :key="s.station">
            <td style="padding: 4px 16px 4px 0;">{{ s.station }}</td>
            <td style="padding: 4px 0; text-align: right; font-variant-numeric: tabular-nums;">{{ s.count }}</td>
          </tr>
          <tr style="border-top: 1px solid #ccc; font-weight: bold;">
            <td style="padding: 4px 16px 4px 0;">In progress</td>
            <td style="padding: 4px 0; text-align: right; font-variant-numeric: tabular-nums;">{{ wipTotal }}</td>
          </tr>
        </tbody>
      </table>
      <p style="margin-top: 12px;">
        <strong>Installed (complete):</strong> {{ installedCount }}
      </p>
      <p><strong>Past due:</strong> {{ dashboard.pastDueCount }}</p>
    </section>

    <section v-if="throughput">
      <h2>Throughput</h2>
      <p>Completed/day: {{ throughput.completedPerDay.toFixed(2) }}</p>
      <p>Due/day: {{ throughput.duePerDay.toFixed(2) }}</p>
      <p>Keeping up: {{ throughput.keepingUp ? 'Yes' : 'No' }}</p>
      <ul>
        <li v-for="day in throughput.daily" :key="day.day">
          {{ day.day }}: {{ day.completed }}
        </li>
      </ul>
    </section>
  </main>
</template>
