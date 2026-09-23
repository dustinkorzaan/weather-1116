import { afterEach, expect, test, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import { BackendWakeGate } from './BackendWakeGate';

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('reveals children once every backend layer answers, without ever showing the wake screen for a fast warm-up', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 200 }));

  render(
    <BackendWakeGate>
      <div>App content</div>
    </BackendWakeGate>
  );

  expect(await screen.findByText('App content')).toBeDefined();
  expect(screen.queryByTestId('backend-wake-screen')).toBeNull();
});

test('shows a blank screen during the 250ms grace period, then the wake screen with one row per layer in order', async () => {
  vi.useFakeTimers();
  vi.spyOn(globalThis, 'fetch').mockReturnValue(new Promise(() => {}));

  render(
    <BackendWakeGate>
      <div>App content</div>
    </BackendWakeGate>
  );

  expect(screen.queryByTestId('backend-wake-screen')).toBeNull();
  expect(screen.queryByText('App content')).toBeNull();

  await act(async () => {
    await vi.advanceTimersByTimeAsync(250);
  });

  expect(screen.getByTestId('backend-wake-screen')).toBeDefined();
  const labels = screen.getAllByText(/waking…$/).map((el) => el.textContent);
  expect(labels).toEqual([
    'API waking…',
    'Worker waking…',
    'MCP App Service waking…',
    'MCP Func App waking…',
    'MCP Python waking…',
    'MCP Node waking…',
    'MVC waking…',
    'Blazor waking…',
  ]);
});

test('keeps a target that answered during the grace period marked ready once the wake screen becomes visible', async () => {
  vi.useFakeTimers();
  // Only the Worker target (whose /Wake ping hits port 8130) ever resolves; every
  // other layer hangs forever, so the gate stays un-warm and the grace-period
  // timer fires.
  vi.spyOn(globalThis, 'fetch').mockImplementation((url) =>
    String(url).includes('8130') ? Promise.resolve(new Response(null, { status: 200 })) : new Promise(() => {})
  );

  render(
    <BackendWakeGate>
      <div>App content</div>
    </BackendWakeGate>
  );

  // Let the Worker ping resolve and mark its target ready before the grace period ends.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });
  expect(screen.getByText('Worker ready')).toBeDefined();

  await act(async () => {
    await vi.advanceTimersByTimeAsync(250);
  });

  expect(screen.getByTestId('backend-wake-screen')).toBeDefined();
  // Must still read "ready", not have been remounted back to "waking…" when the
  // WakeTarget instances transitioned from hidden to visible.
  expect(screen.getByText('Worker ready')).toBeDefined();
});
