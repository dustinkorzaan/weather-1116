import { afterEach, expect, test, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import WakeTarget from './WakeTarget';

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('spins until the ping succeeds, then shows ready and reports the key', async () => {
  let resolvePing;
  vi.spyOn(globalThis, 'fetch').mockReturnValue(
    new Promise((resolve) => {
      resolvePing = resolve;
    })
  );
  const onReady = vi.fn();

  render(<WakeTarget wakeKey="api" label="API" url="http://localhost:8080/Wake" onReady={onReady} />);

  expect(screen.getByText(/API waking/)).toBeDefined();
  expect(document.querySelector('.weather-wake-logo-spin')).toBeTruthy();

  await act(async () => {
    resolvePing(new Response(null, { status: 200 }));
  });

  expect(screen.getByText(/API ready/)).toBeDefined();
  expect(document.querySelector('.weather-wake-logo-spin')).toBeNull();
  expect(onReady).toHaveBeenCalledWith('api');
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
  const onReady = vi.fn();

  render(<WakeTarget wakeKey="api" label="API" url="http://localhost:8080/Wake" onReady={onReady} />);

  await act(() => vi.advanceTimersByTimeAsync(30000));

  expect(callCount).toBeGreaterThan(1);
  expect(onReady).toHaveBeenCalledWith('api');
});
