<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  api,
  stationCardId,
  STATION_ORDER,
  TERMINAL_STATION,
  type DashboardDto,
  type SpoolSummaryDto,
  type ThroughputDto,
  type WipStationCount,
} from '../api';

const route = useRoute();
const router = useRouter();

const dashboard = ref<DashboardDto | null>(null);
const throughput = ref<ThroughputDto | null>(null);
const spools = ref<SpoolSummaryDto[]>([]);
const error = ref('');
const loading = ref(true);

// Installed is terminal (complete), so it is not counted as work-in-progress.
const inFlight = computed<WipStationCount[]>(() =>
  dashboard.value?.wipByStation.filter(s => s.station !== TERMINAL_STATION) ?? []
);
const installedCount = computed(() =>
  dashboard.value?.wipByStation.find(s => s.station === TERMINAL_STATION)?.count ?? 0
);
const wipTotal = computed(() => inFlight.value.reduce((sum, s) => sum + s.count, 0));

// Selected station comes from the URL (?station=) so the view is linkable and
// the browser back button works. Default to the first station in the chain.
const selected = computed(() => {
  const q = route.query.station;
  const candidate = Array.isArray(q) ? q[0] : q;
  return STATION_ORDER.includes(candidate as never)
    ? (candidate as string)
    : STATION_ORDER[0];
});

const spoolsAtSelected = computed(() =>
  spools.value.filter(s => s.currentStation === selected.value)
);

// The accordion holding the spool table opens automatically once a station is
// picked, and can be toggled shut. It starts open if the URL already names one.
const open = ref(false);

function select(station: string) {
  router.replace({ path: '/', query: { station } });
  open.value = true;
}

const dateFmt = new Intl.DateTimeFormat('en-US', {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
});
function formatDue(iso: string) {
  // dueDate is a date-only string (yyyy-MM-dd); parse as local to avoid TZ drift.
  const [y, m, d] = iso.split('-').map(Number);
  return dateFmt.format(new Date(y, m - 1, d));
}

onMounted(async () => {
  open.value = Boolean(route.query.station);
  try {
    const [d, t, list] = await Promise.all([
      api.dashboard(),
      api.throughput(),
      api.spools(),
    ]);
    dashboard.value = d;
    throughput.value = t;
    spools.value = list;
  } catch {
    error.value = 'Unable to load dashboard';
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <p v-if="error" class="error">{{ error }}</p>

  <section v-if="dashboard">
    <div class="card">
      <h2 class="section-title" style="margin:0 0 16px;">Work in progress</h2>

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

      <!-- One-way station flow: arrows between cards show the process direction.
           Picking a station opens the spool accordion below. -->
      <div class="station-flow" style="margin-bottom:20px;">
        <template v-for="(s, i) in inFlight" :key="s.station">
          <button
            :id="stationCardId(s.station)"
            class="tile station-card"
            :class="{ 'is-active': s.station === selected }"
            :data-station="s.station"
            type="button"
            @click="select(s.station)"
          >
            <span class="tile-count">{{ s.count }}</span>
            <span class="tile-label">{{ s.station }}</span>
          </button>
          <span
            v-if="i < inFlight.length - 1"
            class="station-flow-arrow"
            aria-hidden="true"
          >→</span>
        </template>
      </div>

      <!-- Accordion: the per-station spool table, previously its own /wip page. -->
      <div class="accordion">
        <button
          class="accordion-header"
          type="button"
          :aria-expanded="open"
          @click="open = !open"
        >
          <span class="accordion-title">
            {{ selected }} · {{ spoolsAtSelected.length }}
            {{ spoolsAtSelected.length === 1 ? 'spool' : 'spools' }}
          </span>
          <span class="accordion-chevron" :class="{ open }" aria-hidden="true">▸</span>
        </button>

        <div v-show="open" class="accordion-panel">
          <table v-if="spoolsAtSelected.length" class="table">
            <thead>
              <tr>
                <th>Spool</th>
                <th>Package</th>
                <th>Due date</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="spool in spoolsAtSelected" :key="spool.id">
                <td>{{ spool.spoolNumber }}</td>
                <td>{{ spool.packageId }}</td>
                <td>{{ formatDue(spool.dueDate) }}</td>
                <td>
                  <span v-if="spool.isPastDue" class="badge danger">Past due</span>
                  <span v-else class="badge">On track</span>
                </td>
              </tr>
            </tbody>
          </table>

          <p v-else-if="loading" class="muted">Loading…</p>
          <p v-else class="muted">No spools are currently at {{ selected }}.</p>
        </div>
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
