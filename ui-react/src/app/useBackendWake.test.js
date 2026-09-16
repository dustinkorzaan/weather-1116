import { afterEach, expect, test, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useBackendWake } from './useBackendWake';

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('is not warm until the api, mvc, and blazor pings all resolve', async () => {
  let resolveAbout;
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url.includes('/About')) {
      return new Promise((resolve) => {
        resolveAbout = resolve;
      });
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  await waitFor(() => {
    expect(result.current.mvc).toBe(true);
    expect(result.current.blazor).toBe(true);
  });
  expect(result.current.api).toBe(false);
  expect(result.current.isWarm).toBe(false);

  resolveAbout(new Response(null, { status: 200 }));

  await waitFor(() => {
    expect(result.current.isWarm).toBe(true);
  });
});

test('retries a failed ping every ~15s until it succeeds', async () => {
  vi.useFakeTimers();
  let mvcCallCount = 0;
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url.includes('localhost:8100')) {
      mvcCallCount += 1;
      if (mvcCallCount === 1) {
        return Promise.reject(new Error('connection refused'));
      }
      return Promise.resolve(new Response(null, { status: 200 }));
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });
  expect(result.current.mvc).toBe(false);

  await act(async () => {
    await vi.advanceTimersByTimeAsync(15000);
  });

  expect(mvcCallCount).toBeGreaterThan(1);
  expect(result.current.mvc).toBe(true);
});

test('a slow first attempt still wins instead of being cancelled by later retries', async () => {
  vi.useFakeTimers();
  const apiResolvers = [];
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url.includes('/About')) {
      return new Promise((resolve) => {
        apiResolvers.push(resolve);
      });
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  // Let a couple of retry intervals pass -- more requests fire, but none
  // has resolved yet, so none can have won.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(30000);
  });
  expect(apiResolvers.length).toBeGreaterThan(1);
  expect(result.current.api).toBe(false);

  // The very first (still-pending) attempt finally comes back -- it should
  // still count as the win, even though later retries were also in flight.
  await act(async () => {
    apiResolvers[0](new Response(null, { status: 200 }));
    await vi.advanceTimersByTimeAsync(0);
  });

  expect(result.current.api).toBe(true);
});
