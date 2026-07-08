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
export async function fetchAbout() {
  const uiReactNode = createNode('UI React');

  let apiRoot;
  try {
    const response = await fetch('/About');
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`);
    }
    apiRoot = await response.json();
  } catch {
    apiRoot = createNode('API Root', { isHealthy: false });
  }

  const children = [uiReactNode, apiRoot];

  return createNode('UI React Root', {
    isHealthy: computeAggregateHealth(children),
    children,
  });
}
