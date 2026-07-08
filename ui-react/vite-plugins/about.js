import { createRootNode, createSelfNode } from '../src/about/aboutNode.js';

/**
 * Vite plugin exposing an About endpoint on the React host itself (dev + preview servers).
 * Builds: "UI React Root" -> [UI React, API Root -> [API, Core Root -> Core]]
 * by fetching the API's own About endpoint and nesting it under the React root,
 * per the "first child is always the app itself" ordering rule.
 */
export function aboutEndpointPlugin() {
  const handleAboutRequest = async (req, res) => {
    if (req.method !== 'GET') {
      res.statusCode = 405;
      res.end();
      return;
    }

    const reactSelf = createSelfNode('UI React');
    const apiUrl = process.env.WEATHER_URL ?? 'http://localhost:8080';

    let apiRoot;
    try {
      const response = await fetch(`${apiUrl}/About`);
      if (!response.ok) {
        throw new Error(`About request to API failed: ${response.status}`);
      }
      apiRoot = await response.json();
    } catch {
      // API unreachable - report an unhealthy API subtree rather than failing the whole tree.
      apiRoot = createRootNode('API Root', [createSelfNode('API', false)]);
    }

    const reactRoot = createRootNode('UI React Root', [reactSelf, apiRoot]);

    res.setHeader('Content-Type', 'application/json');
    res.end(JSON.stringify(reactRoot));
  };

  return {
    name: 'about-endpoint',
    configureServer(server) {
      server.middlewares.use('/About', handleAboutRequest);
    },
    configurePreviewServer(server) {
      server.middlewares.use('/About', handleAboutRequest);
    },
  };
}
