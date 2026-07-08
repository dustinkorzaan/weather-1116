// Shared About node contract (mirrors Core.about.AboutNode on the .NET side):
//   name (string), isHealthy (bool), version (string|null), buildStart (datetime|null),
//   buildNumber (int|null), children (array)

export function createNode(name, overrides = {}) {
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

export function computeAggregateHealth(nodes) {
  return nodes.every((node) => node.isHealthy && computeAggregateHealth(node.children ?? []));
}

export function buildApiAboutTree({ apiHealthy = true, coreHealthy = true } = {}) {
  const apiNode = createNode('API', { isHealthy: apiHealthy });
  const coreNode = createNode('Core', { isHealthy: coreHealthy });
  const coreRootChildren = [coreNode];
  const coreRoot = createNode('Core Root', {
    isHealthy: computeAggregateHealth(coreRootChildren),
    children: coreRootChildren,
  });
  const apiRootChildren = [apiNode, coreRoot];

  return createNode('API Root', {
    isHealthy: computeAggregateHealth(apiRootChildren),
    children: apiRootChildren,
  });
}

export function buildReactAboutTree(apiRoot) {
  const uiReactNode = createNode('UI React');
  const children = [uiReactNode, apiRoot];

  return createNode('UI React Root', {
    isHealthy: computeAggregateHealth(children),
    children,
  });
}

export async function fetchApiAbout(url) {
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`);
    }

    return await response.json();
  } catch {
    return buildApiAboutTree({ apiHealthy: false, coreHealthy: false });
  }
}

export async function fetchAbout() {
  const response = await fetch('/About');
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.json();
}
