import { createRouter, createWebHistory } from 'vue-router';
import SummaryView from './views/SummaryView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'summary', component: SummaryView },
    // The WIP detail table now lives in an accordion on the root page. Keep the
    // old path working by redirecting to it, preserving any ?station= query so
    // existing deep links still open the right station.
    { path: '/wip', redirect: to => ({ path: '/', query: to.query }) },
  ],
});
