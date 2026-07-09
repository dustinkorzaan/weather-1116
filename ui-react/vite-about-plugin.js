import { getAboutTree } from './src/services/about.js';

function aboutEndpointMiddleware(weatherUrl) {
  return async (req, res, next) => {
    const path = req.url?.split('?')[0];
    if (path !== '/About' || req.method !== 'GET') {
      next();
      return;
    }

    try {
      const root = await getAboutTree(weatherUrl);
      res.setHeader('Content-Type', 'application/json');
      res.statusCode = 200;
      res.end(JSON.stringify(root));
    } catch {
      res.statusCode = 500;
      res.end();
    }
  };
}

/** Serves GET /About on the React dev/preview server with the nested UI React tree. */
export function aboutEndpointPlugin(weatherUrl = process.env.WEATHER_URL) {
  const middleware = aboutEndpointMiddleware(weatherUrl);

  return {
    name: 'about-endpoint',
    configureServer(server) {
      server.middlewares.use(middleware);
    },
    configurePreviewServer(server) {
      server.middlewares.use(middleware);
    },
  };
}
