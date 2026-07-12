// Shared About node contract (mirrors Core.about.AboutNode on the .NET side):
//   name (string), isHealthy (bool), version (string|null), buildStart (datetime|null),
//   buildNumber (int|null), children (array)

function createNode(name, overrides = {}) {
  return {
    name,
    isHealthy: true,
    version: null,
    buildStart: null,
    buildNumber: null,
    children: [],
    ...overrides,
  };
}

function computeAggregateHealth(nodes) {
  return nodes.every((node) => node.isHealthy && computeAggregateHealth(node.children ?? []));
}

/**
 * Builds the "UI React Root" About tree. The first child is always the UI React app
 * itself, followed by the API's own About tree (which nests Core beneath it).
 */
export function buildUiReactRoot(apiRoot) {
  const uiReactNode = createNode('UI React');
  const children = [uiReactNode, apiRoot];

  return createNode('UI React Root', {
    isHealthy: computeAggregateHealth(children),
    children,
  });
}

export async function fetchApiAbout(weatherUrl) {
  const viteApiUrl = typeof import.meta !== 'undefined' && import.meta.env
    ? import.meta.env.VITE_WEATHER1116_API_URL
    : undefined;
  const baseUrl = weatherUrl ?? viteApiUrl ?? 'http://localhost:8080';
  try {
    const response = await fetch(`${baseUrl}/About`);
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`);
    }
    return await response.json();
  } catch {
    return createNode('API Root', { isHealthy: false });
  }
}

/** Builds the full UI React About tree (used by the React host /About endpoint). */
export async function getAboutTree(weatherUrl) {
  const apiRoot = await fetchApiAbout(weatherUrl);
  return buildUiReactRoot(apiRoot);
}

/** Fetches the About tree from the React host's /About endpoint. */
export async function fetchAbout() {
  const response = await fetch('/About');
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}
