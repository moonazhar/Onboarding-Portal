import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // The browser only talks to this dev server, which forwards /api to the local API: same origin, no CORS.
      // secure: false accepts the local ASP.NET Core development certificate, even if it isn't trusted.
      '/api': { target: 'https://localhost:7180', secure: false },
    },
  },
  build: {
    // Source maps are not published (docs/standards/frontend.md §8).
    sourcemap: false,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/testSetup.ts'],
  },
});
