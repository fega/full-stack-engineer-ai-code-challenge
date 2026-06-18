<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import {
  api,
  TERMINAL_STATION,
  type DashboardDto,
  type ThroughputDto,
} from '../api';

const dashboard = ref<DashboardDto | null>(null);
const throughput = ref<ThroughputDto | null>(null);
const error = ref('');

// Installed is terminal (complete), so it is not counted as work-in-progress.
const inFlight = computed(() =>
  dashboard.value?.wipByStation.filter(s => s.station !== TERMINAL_STATION) ?? []
);
const installedCount = computed(() =>
  dashboard.value?.wipByStation.find(s => s.station === TERMINAL_STATION)?.count ?? 0
);
const wipTotal = computed(() => inFlight.value.reduce((sum, s) => sum + s.count, 0));

onMounted(async () => {
  try {
    const [d, t] = await Promise.all([api.dashboard(), api.throughput()]);
    dashboard.value = d;
    throughput.value = t;
  } catch {
    error.value = 'Unable to load dashboard';
  }
});
</script>

<template>
  <p v-if="error" class="error">{{ error }}</p>

  <section v-if="dashboard">
    <div class="card">
      <div style="display:flex; align-items:center; justify-content:space-between; gap:16px; margin-bottom:16px;">
        <h2 class="section-title" style="margin:0;">Work in progress</h2>
        <RouterLink class="btn" to="/wip">View WIP detail →</RouterLink>
      </div>

      <div class="stat-row" style="margin-bottom:20px;">
        <div class="stat">
          <span class="stat-value">{{ wipTotal }}</span>
          <span class="stat-label">Spools in progress</span>
        </div>
        <div class="stat">
          <span class="stat-value ok">{{ installedCount }}</span>
          <span class="stat-label">Installed (complete)</span>
        </div>
        <div class="stat">
          <span class="stat-value" :class="{ danger: dashboard.pastDueCount > 0 }">
            {{ dashboard.pastDueCount }}
          </span>
          <span class="stat-label">Past due</span>
        </div>
      </div>

      <div class="grid">
        <RouterLink
          v-for="s in inFlight"
          :key="s.station"
          class="tile"
          :to="{ path: '/wip', query: { station: s.station } }"
        >
          <span class="tile-count">{{ s.count }}</span>
          <span class="tile-label">{{ s.station }}</span>
        </RouterLink>
      </div>
    </div>

    <div v-if="throughput" class="card">
      <h2 class="section-title">Throughput (14-day)</h2>
      <div class="stat-row">
        <div class="stat">
          <span class="stat-value">{{ throughput.completedPerDay.toFixed(2) }}</span>
          <span class="stat-label">Completed / day</span>
        </div>
        <div class="stat">
          <span class="stat-value">{{ throughput.duePerDay.toFixed(2) }}</span>
          <span class="stat-label">Due / day</span>
        </div>
        <div class="stat">
          <span class="stat-value" :class="throughput.keepingUp ? 'ok' : 'danger'">
            {{ throughput.keepingUp ? 'Yes' : 'No' }}
          </span>
          <span class="stat-label">Keeping up</span>
        </div>
      </div>
    </div>
  </section>

  <p v-else-if="!error" class="muted">Loading…</p>
</template>
