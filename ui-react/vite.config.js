import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { buildReactAboutTree, fetchApiAbout } from "./src/services/about.js";

const weatherUrl = process.env.WEATHER_URL ?? "http://localhost:8080";

function aboutEndpointPlugin() {
  const middleware = async (req, res, next) => {
    const pathname = new URL(req.url ?? "/", "http://localhost").pathname;
    if (pathname !== "/About") {
      next();
      return;
    }

    const apiRoot = await fetchApiAbout(`${weatherUrl}/About`);
    const root = buildReactAboutTree(apiRoot);

    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify(root));
  };

  return {
    name: "about-endpoint",
    configureServer(server) {
      server.middlewares.use(middleware);
    },
    configurePreviewServer(server) {
      server.middlewares.use(middleware);
    },
  };
}

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
