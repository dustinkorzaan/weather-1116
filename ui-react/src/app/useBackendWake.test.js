import { afterEach, expect, test, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useBackendWake } from './useBackendWake';

afterEach(() => {
  vi.restoreAllMocks();
});

function mockWakeFetch() {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 200 }));
}

test('is not warm until the api, mvc, and blazor pings all settle', async () => {
  mockWakeFetch();
  let resolveAbout;
  const loadAbout = vi.fn(() => new Promise((resolve) => {
    resolveAbout = resolve;
  }));

  const { result } = renderHook(() => useBackendWake(loadAbout));

  expect(result.current).toBe(false);

  resolveAbout();

  await waitFor(() => {
    expect(result.current).toBe(true);
  });
});

test('becomes warm even when a ping rejects', async () => {
  vi.spyOn(globalThis, 'fetch').mockRejectedValue(new Error('network down'));
  const loadAbout = vi.fn().mockResolvedValue(undefined);

  const { result } = renderHook(() => useBackendWake(loadAbout));

  await waitFor(() => {
    expect(result.current).toBe(true);
  });
});
