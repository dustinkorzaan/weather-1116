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
    'MVC waking…',
    'Blazor waking…',
  ]);
});
