import { afterEach, expect, test, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import App from './App';

afterEach(() => {
  vi.restoreAllMocks();
});

test('renders weather app title and loaded data', async () => {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = String(input);

    if (url.endsWith('/Home/Hello')) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ requestResponse: 'Hello from test API.' }),
      };
    }

    if (url.endsWith('/weatherforecast')) {
      return {
        ok: true,
        status: 200,
        json: async () => ([
          {
            date: '2026-07-12',
            temperatureK: 300.15,
            summary: 'Warm',
          },
        ]),
      };
    }

    return {
      ok: false,
      status: 404,
      json: async () => ({}),
    };
  });

  render(<App />);

  expect(await screen.findByRole('heading', { name: /weather react/i })).toBeDefined();
  expect(await screen.findByText('Hello from test API.')).toBeDefined();
  expect(await screen.findByText('Warm')).toBeDefined();
});
