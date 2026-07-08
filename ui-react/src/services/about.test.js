import { afterEach, describe, expect, test, vi } from 'vitest';
import { buildApiAboutTree, buildReactAboutTree, fetchAbout, fetchApiAbout } from './about';

describe('about service helpers', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  test('buildReactAboutTree returns a single UI React root whose first child is UI React itself', () => {
    const apiRoot = buildApiAboutTree();
    const root = buildReactAboutTree(apiRoot);

    expect(root.name).toBe('UI React Root');
    expect(root.children[0].name).toBe('UI React');
    expect(root.children[1]).toEqual(apiRoot);
    expect(root.isHealthy).toBe(true);
  });

  test('buildReactAboutTree aggregates isHealthy to false when a descendant is unhealthy', () => {
    const apiRoot = buildApiAboutTree({ apiHealthy: true, coreHealthy: false });
    const root = buildReactAboutTree(apiRoot);

    expect(root.isHealthy).toBe(false);
  });

  test('fetchApiAbout falls back to the full unhealthy API subtree when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')));

    const apiRoot = await fetchApiAbout('http://localhost:8080/About');

    expect(apiRoot.name).toBe('API Root');
    expect(apiRoot.children[0].name).toBe('API');
    expect(apiRoot.children[1].name).toBe('Core Root');
    expect(apiRoot.children[1].children[0].name).toBe('Core');
    expect(apiRoot.isHealthy).toBe(false);
  });

  test('fetchAbout returns the React host /About response as a single root object', async () => {
    const root = buildReactAboutTree(buildApiAboutTree());

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(root),
      })
    );

    await expect(fetchAbout()).resolves.toEqual(root);
  });
});
