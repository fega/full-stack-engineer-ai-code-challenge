// Shared API types and helpers for the Stratus Fab Tracker dashboard.

export type WipStationCount = { station: string; count: number };

export type DashboardDto = {
  wipByStation: WipStationCount[];
  pastDueCount: number;
};

export type ThroughputDayDto = { day: string; completed: number };
export type ThroughputDto = {
  daily: ThroughputDayDto[];
  completedPerDay: number;
  duePerDay: number;
  keepingUp: boolean;
};

export type SpoolSummaryDto = {
  id: string;
  spoolNumber: string;
  packageId: string;
  dueDate: string;
  currentStation: string;
  isPastDue: boolean;
};

// Domain order of the fabrication chain. The API already returns WIP counts in
// this order, but the UI also relies on it to label and sort the spool groups.
export const STATION_ORDER = [
  'Detailing',
  'Cut',
  'Weld',
  'QC',
  'Shipped',
  'Installed',
] as const;

// Installed is the terminal station: those spools are complete, not in-flight WIP.
export const TERMINAL_STATION = 'Installed';

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Request failed: ${res.status}`);
  return res.json() as Promise<T>;
}

export const api = {
  dashboard: () => getJson<DashboardDto>('/api/dashboard'),
  throughput: () => getJson<ThroughputDto>('/api/throughput'),
  spools: () => getJson<SpoolSummaryDto[]>('/api/spools'),
};
