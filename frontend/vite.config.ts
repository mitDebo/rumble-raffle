/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      // Lets shadcn/ui components (and our own code) import via "@/..."
      // instead of long relative paths — matches the alias configured in
      // tsconfig.json/tsconfig.app.json and components.json.
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    proxy: {
      // Local dev only — production routes /api to the backend via nginx
      // on the same origin instead. Port must match the backend's
      // launchSettings.json dev profile. ws: true is required for SignalR
      // (1.8) — without it, Vite's proxy only forwards plain HTTP and lets
      // the WebSocket upgrade request fall through unhandled.
      '/api': {
        target: 'http://localhost:5100',
        ws: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './vitest.setup.ts',
  },
})
