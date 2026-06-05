<script setup lang="ts">
import { onMounted, ref } from 'vue';

type DashboardDto = {
  wipByStation: Record<string, number>;
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
      <ul>
        <li v-for="(count, station) in dashboard.wipByStation" :key="station">
          {{ station }}: {{ count }}
        </li>
      </ul>
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
