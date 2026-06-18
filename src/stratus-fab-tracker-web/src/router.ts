import { createRouter, createWebHistory } from 'vue-router';
import SummaryView from './views/SummaryView.vue';
import WipDetailView from './views/WipDetailView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'summary', component: SummaryView },
    { path: '/wip', name: 'wip-detail', component: WipDetailView },
  ],
});
