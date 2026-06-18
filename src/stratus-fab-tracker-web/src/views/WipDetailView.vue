<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  api,
  STATION_ORDER,
  TERMINAL_STATION,
  type SpoolSummaryDto,
  type WipStationCount,
} from '../api';

const route = useRoute();
const router = useRouter();

const counts = ref<WipStationCount[]>([]);
const spools = ref<SpoolSummaryDto[]>([]);
const error = ref('');
const loading = ref(true);

// Stations in domain order, falling back to the canonical order if the API is slow.
const stations = computed<WipStationCount[]>(() =>
  counts.value.length
    ? counts.value
    : STATION_ORDER.map(station => ({ station, count: 0 }))
);

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

function select(station: string) {
  router.replace({ path: '/wip', query: { station } });
}

// Stable, station-derived id so individual station cards can be targeted later.
const stationId = (station: string) =>
  `station-card-${station.toLowerCase().replace(/\s+/g, '-')}`;

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
  try {
    const [dash, list] = await Promise.all([api.dashboard(), api.spools()]);
    counts.value = dash.wipByStation;
    spools.value = list;
  } catch {
    error.value = 'Unable to load WIP detail';
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <p v-if="error" class="error">{{ error }}</p>

  <section v-else>
    <h2 class="section-title">WIP per station — pick a station to see its spools</h2>

    <!-- One-way station flow: arrows between cards show the process direction. -->
    <div class="station-flow" style="margin-bottom:24px;">
      <template v-for="(s, i) in stations" :key="s.station">
        <button
          :id="stationId(s.station)"
          class="tile station-card"
          :class="{ 'is-active': s.station === selected, 'is-terminal': s.station === TERMINAL_STATION }"
          :data-station="s.station"
          type="button"
          @click="select(s.station)"
        >
          <span class="tile-count">{{ s.count }}</span>
          <span class="tile-label">{{ s.station }}</span>
          <span v-if="s.station === TERMINAL_STATION" class="tile-tag">Complete</span>
        </button>
        <span
          v-if="i < stations.length - 1"
          class="station-flow-arrow"
          aria-hidden="true"
        >→</span>
      </template>
    </div>

    <div class="card">
      <h3 class="section-title">
        {{ selected }} · {{ spoolsAtSelected.length }}
        {{ spoolsAtSelected.length === 1 ? 'spool' : 'spools' }}
      </h3>

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
  </section>
</template>
