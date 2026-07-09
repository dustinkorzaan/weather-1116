import { describe, expect, test } from 'vitest';
import { buildUiReactRoot } from './about';

describe('buildUiReactRoot', () => {
  test('returns a single "UI React Root" node whose first child is UI React itself', () => {
    const apiRoot = {
      name: 'API Root',
      isHealthy: true,
      version: null,
      buildStart: null,
      buildNumber: null,
      children: [
        { name: 'API', isHealthy: true, version: null, buildStart: null, buildNumber: null, children: [] },
        {
          name: 'Core Root',
          isHealthy: true,
          version: null,
          buildStart: null,
          buildNumber: null,
          children: [
            { name: 'Core', isHealthy: true, version: null, buildStart: null, buildNumber: null, children: [] },
          ],
        },
      ],
    };

    const root = buildUiReactRoot(apiRoot);

    expect(root.name).toBe('UI React Root');
    expect(root.children[0].name).toBe('UI React');
    expect(root.children[1]).toEqual(apiRoot);
    expect(root.isHealthy).toBe(true);
  });

  test('aggregates isHealthy to false when a descendant is unhealthy', () => {
    const apiRoot = {
      name: 'API Root',
      isHealthy: true,
      version: null,
      buildStart: null,
      buildNumber: null,
      children: [
        { name: 'API', isHealthy: true, version: null, buildStart: null, buildNumber: null, children: [] },
        {
          name: 'Core Root',
          isHealthy: false,
          version: null,
          buildStart: null,
          buildNumber: null,
          children: [
            { name: 'Core', isHealthy: false, version: null, buildStart: null, buildNumber: null, children: [] },
          ],
        },
      ],
    };

    const root = buildUiReactRoot(apiRoot);

    expect(root.isHealthy).toBe(false);
  });

  test('marks root unhealthy when API root node is unhealthy', () => {
    const apiRoot = {
      name: 'API Root',
      isHealthy: false,
      version: null,
      buildStart: null,
      buildNumber: null,
      children: [],
    };

    const root = buildUiReactRoot(apiRoot);

    expect(root.name).toBe('UI React Root');
    expect(root.children[0].name).toBe('UI React');
    expect(root.children[1].name).toBe('API Root');
    expect(root.children[1].isHealthy).toBe(false);
    expect(root.isHealthy).toBe(false);
  });
});
