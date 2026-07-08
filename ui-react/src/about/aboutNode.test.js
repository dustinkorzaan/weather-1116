import { describe, expect, test } from 'vitest';
import { createSelfNode, createRootNode, isSubtreeHealthy } from './aboutNode';

describe('aboutNode', () => {
  test('createSelfNode defaults to healthy with null metadata', () => {
    const node = createSelfNode('UI React');

    expect(node).toEqual({
      name: 'UI React',
      isHealthy: true,
      version: null,
      buildStart: null,
      buildNumber: null,
      children: [],
    });
  });

  test('createRootNode aggregates health as true when all descendants are healthy', () => {
    const self = createSelfNode('UI React');
    const apiRoot = createRootNode('API Root', [createSelfNode('API')]);
    const root = createRootNode('UI React Root', [self, apiRoot]);

    expect(root.isHealthy).toBe(true);
    expect(root.children[0]).toBe(self);
    expect(root.children[1]).toBe(apiRoot);
  });

  test('createRootNode aggregates health as false when any descendant is unhealthy', () => {
    const self = createSelfNode('UI React');
    const unhealthyApi = createSelfNode('API', false);
    const apiRoot = createRootNode('API Root', [unhealthyApi]);
    const root = createRootNode('UI React Root', [self, apiRoot]);

    expect(root.isHealthy).toBe(false);
  });

  test('isSubtreeHealthy checks the whole subtree recursively', () => {
    const healthyLeaf = createSelfNode('Core');
    const healthySubtree = createRootNode('Core Root', [healthyLeaf]);
    expect(isSubtreeHealthy(healthySubtree)).toBe(true);

    const unhealthyLeaf = createSelfNode('Core', false);
    const unhealthySubtree = createRootNode('Core Root', [unhealthyLeaf]);
    expect(isSubtreeHealthy(unhealthySubtree)).toBe(false);
  });
});
