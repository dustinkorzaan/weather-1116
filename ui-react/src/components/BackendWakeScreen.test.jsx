import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import BackendWakeScreen from './BackendWakeScreen';

test('shows a spinning logo per layer, stopping the ones already warm', () => {
  render(<BackendWakeScreen statuses={{ api: true, mvc: false, blazor: false }} />);

  expect(screen.getByText(/API ready/)).toBeDefined();
  expect(screen.getByText(/MVC waking/)).toBeDefined();
  expect(screen.getByText(/Blazor waking/)).toBeDefined();

  const spinners = document.querySelectorAll('.weather-wake-logo-spin');
  expect(spinners).toHaveLength(2);
});

test('spins all three logos when nothing has answered yet', () => {
  render(<BackendWakeScreen />);

  expect(screen.getByText(/API waking/)).toBeDefined();
  expect(screen.getByText(/MVC waking/)).toBeDefined();
  expect(screen.getByText(/Blazor waking/)).toBeDefined();
  expect(document.querySelectorAll('.weather-wake-logo-spin')).toHaveLength(3);
});
