import { defineConfig } from 'vite';

// Bind only to loopback so the dev/preview servers are never reachable from
// other machines on the network.
const LOCAL_ONLY = { host: '127.0.0.1', strictPort: true };

export default defineConfig({
  base: './',
  server: { ...LOCAL_ONLY, port: 5173 },
  preview: { ...LOCAL_ONLY, port: 4173 },
  test: {
    environment: 'node',
    include: ['tests/**/*.test.js'],
  },
});
