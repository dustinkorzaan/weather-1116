import { afterEach, describe, expect, test, vi } from 'vitest';
import { fetchAbout } from './about';

describe('fetchAbout', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  test('returns a single "UI React Root" node whose first child is UI React itself', async () => {
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

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(apiRoot),
      })
    );

    const root = await fetchAbout();

    expect(root.name).toBe('UI React Root');
    expect(root.children[0].name).toBe('UI React');
    expect(root.children[1]).toEqual(apiRoot);
    expect(root.isHealthy).toBe(true);
  });

  test('aggregates isHealthy to false when a descendant is unhealthy', async () => {
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

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(apiRoot),
      })
    );

    const root = await fetchAbout();

    expect(root.isHealthy).toBe(false);
  });

  test('falls back to an unhealthy API Root node when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')));

    const root = await fetchAbout();

    expect(root.name).toBe('UI React Root');
    expect(root.children[0].name).toBe('UI React');
    expect(root.children[1].name).toBe('API Root');
    expect(root.children[1].isHealthy).toBe(false);
    expect(root.isHealthy).toBe(false);
  });
});
