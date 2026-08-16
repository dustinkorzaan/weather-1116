import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
    },
  },
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
      '/Geo': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/Chat1a': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/Chat1b': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/Chat2a': {
        target: process.env.VITE_API_DOTNET_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/Chat2b': {
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
