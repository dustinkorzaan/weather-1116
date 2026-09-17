import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BackendWakeGate } from './BackendWakeGate';

afterEach(() => {
  vi.restoreAllMocks();
});

test('shows the wake screen and withholds children until every backend layer answers', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 200 }));

  render(
    <BackendWakeGate>
      <div>App content</div>
    </BackendWakeGate>
  );

  expect(screen.getByTestId('backend-wake-screen')).toBeDefined();
  expect(screen.queryByText('App content')).toBeNull();

  await waitFor(() => {
    expect(screen.getByText('App content')).toBeDefined();
  });
  expect(screen.queryByTestId('backend-wake-screen')).toBeNull();
});
