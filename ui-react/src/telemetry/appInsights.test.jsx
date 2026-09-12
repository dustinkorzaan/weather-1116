import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TelemetryProvider } from './appInsights';

function Boom() {
  throw new Error('boom');
}

test('renders children normally', () => {
  render(
    <TelemetryProvider>
      <p>hello</p>
    </TelemetryProvider>
  );

  expect(screen.getByText('hello')).toBeDefined();
});

test('catches render errors without a connection string configured', () => {
  // No VITE_APPINSIGHTS_CONNECTION_STRING in the test env, so appInsights is
  // never initialized -- crash handling must still work (this is what regressed
  // when AppInsightsErrorBoundary was wired to call trackException unconditionally).
  render(
    <TelemetryProvider>
      <Boom />
    </TelemetryProvider>
  );

  expect(screen.getByText('Something went wrong.')).toBeDefined();
});
