import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
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
      '/About': {
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
