import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/Home': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/About': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/AIWeather': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
  },
})
