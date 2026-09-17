import { afterEach, expect, test, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useWakeTargets } from './useWakeTargets';

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('marks a target awake once its ping succeeds', async () => {
  let resolvePing;
  vi.spyOn(globalThis, 'fetch').mockReturnValue(
    new Promise((resolve) => {
      resolvePing = resolve;
    })
  );

  const { result } = renderHook(() => useWakeTargets([{ key: 'api', url: 'http://localhost:8080/Wake' }]));

  expect(result.current).toEqual({ api: false });

  await act(async () => {
    resolvePing(new Response(null, { status: 200 }));
  });

  expect(result.current).toEqual({ api: true });
});

test('retries a failed ping every ~30s until it succeeds', async () => {
  vi.useFakeTimers();
  let callCount = 0;
  vi.spyOn(globalThis, 'fetch').mockImplementation(() => {
    callCount += 1;
    return callCount === 1
      ? Promise.reject(new Error('connection refused'))
      : Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useWakeTargets([{ key: 'api', url: 'http://localhost:8080/Wake' }]));

  await act(() => vi.advanceTimersByTimeAsync(30000));

  expect(callCount).toBeGreaterThan(1);
  expect(result.current).toEqual({ api: true });
});

test('tracks each target independently', async () => {
  vi.spyOn(globalThis, 'fetch').mockImplementation((url) =>
    String(url).includes('worker') ? Promise.resolve(new Response(null, { status: 200 })) : new Promise(() => {})
  );

  const { result } = renderHook(() =>
    useWakeTargets([
      { key: 'api', url: 'http://localhost:8080/Wake' },
      { key: 'worker', url: 'http://localhost:8130/worker/Wake' },
    ])
  );

  await act(async () => {});

  expect(result.current).toEqual({ api: false, worker: true });
});
