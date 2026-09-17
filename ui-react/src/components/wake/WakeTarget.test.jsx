import { afterEach, expect, test, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import WakeTarget from './WakeTarget';

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('shows a spinning logo and "waking" label until the ping succeeds, then calls onReady', async () => {
  let resolvePing;
  vi.spyOn(globalThis, 'fetch').mockReturnValue(
    new Promise((resolve) => {
      resolvePing = resolve;
    })
  );
  const onReady = vi.fn();

  render(<WakeTarget label="API" url="http://localhost:8080/Wake" onReady={onReady} />);

  expect(screen.getByText(/API waking/)).toBeDefined();
  expect(document.querySelector('.weather-wake-logo-spin')).toBeTruthy();
  expect(onReady).not.toHaveBeenCalled();

  await act(async () => {
    resolvePing(new Response(null, { status: 200 }));
  });

  await waitFor(() => {
    expect(screen.getByText(/API ready/)).toBeDefined();
  });
  expect(document.querySelector('.weather-wake-logo-spin')).toBeNull();
  expect(onReady).toHaveBeenCalledTimes(1);
});

test('pings the given url in no-cors mode', () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 200 }));

  render(<WakeTarget label="MVC" url="http://localhost:8100" onReady={() => {}} />);

  expect(fetchMock).toHaveBeenCalledWith('http://localhost:8100', { mode: 'no-cors' });
});

test('retries a failed ping every ~30s until it succeeds', async () => {
  vi.useFakeTimers();
  let callCount = 0;
  vi.spyOn(globalThis, 'fetch').mockImplementation(() => {
    callCount += 1;
    if (callCount === 1) {
      return Promise.reject(new Error('connection refused'));
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });
  const onReady = vi.fn();

  render(<WakeTarget label="API" url="http://localhost:8080/Wake" onReady={onReady} />);

  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });
  expect(onReady).not.toHaveBeenCalled();

  await act(async () => {
    await vi.advanceTimersByTimeAsync(30000);
  });

  expect(callCount).toBeGreaterThan(1);
  expect(onReady).toHaveBeenCalledTimes(1);
});

test('a slow first attempt still wins instead of being cancelled by later retries', async () => {
  vi.useFakeTimers();
  const resolvers = [];
  vi.spyOn(globalThis, 'fetch').mockImplementation(
    () =>
      new Promise((resolve) => {
        resolvers.push(resolve);
      })
  );
  const onReady = vi.fn();

  render(<WakeTarget label="API" url="http://localhost:8080/Wake" onReady={onReady} />);

  // Let a couple of retry intervals pass -- more requests fire, but none has
  // resolved yet, so none can have won.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(60000);
  });
  expect(resolvers.length).toBeGreaterThan(1);
  expect(onReady).not.toHaveBeenCalled();

  // The very first (still-pending) attempt finally comes back -- it should still
  // count as the win, even though later retries were also in flight.
  await act(async () => {
    resolvers[0](new Response(null, { status: 200 }));
    await vi.advanceTimersByTimeAsync(0);
  });

  expect(onReady).toHaveBeenCalledTimes(1);
});
