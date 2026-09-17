import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import WakeTarget from './WakeTarget';

test('shows the spinner and "waking…" while not awake', () => {
  render(<WakeTarget label="API" isAwake={false} />);

  expect(screen.getByText(/API waking/)).toBeDefined();
  expect(document.querySelector('.weather-wake-logo-spin')).toBeTruthy();
});

test('shows "ready" with no spinner once awake', () => {
  render(<WakeTarget label="API" isAwake={true} />);

  expect(screen.getByText(/API ready/)).toBeDefined();
  expect(document.querySelector('.weather-wake-logo-spin')).toBeNull();
});
