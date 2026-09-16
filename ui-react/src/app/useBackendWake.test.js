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

test('retries a failed ping every ~30s until it succeeds', async () => {
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
    await vi.advanceTimersByTimeAsync(30000);
  });

  expect(mvcCallCount).toBeGreaterThan(1);
  expect(result.current.mvc).toBe(true);
});

test('a slow first attempt is not overlapped at 30s/60s and still wins when it resolves', async () => {
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

  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });
  expect(apiResolvers).toHaveLength(1);

  // Interval ticks must not start a second /About while the first is still
  // in flight -- that would fan the mesh out again for a merely slow wake.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(60000);
  });
  expect(apiResolvers).toHaveLength(1);
  expect(result.current.api).toBe(false);

  await act(async () => {
    apiResolvers[0](new Response(null, { status: 200 }));
    await vi.advanceTimersByTimeAsync(0);
  });

  expect(result.current.api).toBe(true);
});

test('clears the retry interval as soon as a ping succeeds', async () => {
  vi.useFakeTimers();
  const clearIntervalSpy = vi.spyOn(globalThis, 'clearInterval');
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
    Promise.resolve(new Response(null, { status: 200 }))
  );

  const { result } = renderHook(() => useBackendWake());

  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });

  expect(result.current.isWarm).toBe(true);
  expect(clearIntervalSpy).toHaveBeenCalled();
  const callsAfterWarm = fetchMock.mock.calls.length;

  await act(async () => {
    await vi.advanceTimersByTimeAsync(60000);
  });
  expect(fetchMock.mock.calls.length).toBe(callsAfterWarm);
});
