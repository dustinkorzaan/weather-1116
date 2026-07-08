/**
 * Shared About response contract used by every About endpoint (React host, API, MVC).
 * Each node: { name, isHealthy, version, buildStart, buildNumber, children }.
 */

/**
 * Creates a leaf node representing an app or dependency itself.
 * Version/build metadata is intentionally left null for now (real CI/CD metadata is out of scope).
 */
export function createSelfNode(name, isHealthy = true) {
  return {
    name,
    isHealthy,
    version: null,
    buildStart: null,
    buildNumber: null,
    children: [],
  };
}

/**
 * Creates a root node. The first entry in `children` must be the self node for that app,
 * followed by any dependency subtrees, in deterministic order.
 * Root health is the aggregate of all descendant health (all true => true).
 */
export function createRootNode(name, children) {
  return {
    name,
    isHealthy: children.every(isSubtreeHealthy),
    version: null,
    buildStart: null,
    buildNumber: null,
    children,
  };
}

export function isSubtreeHealthy(node) {
  return Boolean(node) && node.isHealthy && (node.children ?? []).every(isSubtreeHealthy);
}
