// Shared About node contract (mirrors Core.about.AboutNode on the .NET side):
//   name (string), isHealthy (bool), version (string|null), buildStart (datetime|null),
//   buildNumber (int|null), buildBranchName (string|null), children (array)

function createNode(name, overrides = {}) {
  return {
    name,
    isHealthy: true,
    version: null,
    buildStart: resolveBuildStart(),
    buildNumber: resolveBuildNumber(),
    buildBranchName: resolveBuildBranchName(),
    children: [],
    ...overrides,
  };
}

function resolveBuildNumber() {
  const viteValue = typeof import.meta !== 'undefined' && import.meta.env
    ? import.meta.env.VITE_BUILD_NUMBER
    : undefined;
  const nodeValue = typeof process !== 'undefined' && process.env
    ? process.env.VITE_BUILD_NUMBER
    : undefined;
  const rawValue = viteValue ?? nodeValue;
  const value = Number.parseInt(rawValue, 10);
  return Number.isFinite(value) ? value : null;
}

function resolveBuildStart() {
  const viteValue = typeof import.meta !== 'undefined' && import.meta.env
    ? import.meta.env.VITE_BUILD_START
    : undefined;
  const nodeValue = typeof process !== 'undefined' && process.env
    ? process.env.VITE_BUILD_START
    : undefined;
  return viteValue ?? nodeValue ?? null;
}

function resolveBuildBranchName() {
  const viteValue = typeof import.meta !== 'undefined' && import.meta.env
    ? import.meta.env.VITE_BUILD_BRANCH_NAME
    : undefined;
  const nodeValue = typeof process !== 'undefined' && process.env
    ? process.env.VITE_BUILD_BRANCH_NAME
    : undefined;
  const rawValue = viteValue ?? nodeValue;
  return rawValue || null;
}

function computeAggregateHealth(nodes) {
  return nodes.every((node) => node.isHealthy && computeAggregateHealth(node.children ?? []));
}

/**
 * Builds the "UI React Root" About tree. The first child is always the UI React app
 * itself, followed by the API's own About tree.
 */
export function buildUiReactRoot(apiRoot) {
  const uiReactNode = createNode('UI React');
  const children = [uiReactNode, apiRoot];

  return createNode('UI React Root', {
    isHealthy: computeAggregateHealth(children),
    children,
    buildStart: null,
    buildNumber: null,
    buildBranchName: null,
  });
}
