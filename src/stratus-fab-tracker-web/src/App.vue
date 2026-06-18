<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router';
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <h1>Stratus Fab Tracker</h1>
      <nav class="app-nav">
        <RouterLink to="/">Overview</RouterLink>
        <RouterLink to="/wip">WIP detail</RouterLink>
      </nav>
    </header>
    <RouterView />
  </div>
</template>
