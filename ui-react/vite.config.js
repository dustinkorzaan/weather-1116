import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { aboutEndpointPlugin } from "./vite-plugins/about.js";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), aboutEndpointPlugin()],
  server: {
    proxy: {
      '/Home': {
        target: process.env.WEATHER_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/weatherforecast': {
        target: process.env.WEATHER_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
  },
})
