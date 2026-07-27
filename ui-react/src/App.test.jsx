import { afterEach, expect, test, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import App, { AboutTreeNode } from './App';
import { weatherApi } from './services/weatherApi';

function createTestStore() {
  return configureStore({
    reducer: {
      [weatherApi.reducerPath]: weatherApi.reducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(weatherApi.middleware),
  });
}

afterEach(() => {
  vi.restoreAllMocks();
});

test('renders weather app title and loaded data', async () => {
  const store = createTestStore();

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = input instanceof Request ? input.url : String(input);

    if (url.endsWith('/Home/Hello')) {
      return new Response(
        JSON.stringify({ requestResponse: 'Hello from test API.' }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }
      );
    }

    if (url.endsWith('/weatherforecast')) {
      return new Response(
        JSON.stringify([
          {
            date: '2026-07-12',
            temperatureK: 300.15,
            summary: 'Warm',
          },
        ]),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }
      );
    }

    return new Response(JSON.stringify({}), {
      status: 404,
      headers: { 'Content-Type': 'application/json' },
    });
  });

  render(
    <Provider store={store}>
      <MemoryRouter>
        <App />
      </MemoryRouter>
    </Provider>
  );

  expect(await screen.findByRole('heading', { name: /weather react/i })).toBeDefined();
  expect(await screen.findByText('Hello from test API.')).toBeDefined();
  expect(await screen.findByLabelText(/location:/i)).toBeDefined();
  expect(await screen.findByRole('button', { name: /get current ai weather/i })).toBeDefined();
  expect(await screen.findByText('Warm')).toBeDefined();
});

test('renders a public message in the About tree', () => {
  render(
    <AboutTreeNode
      node={{
        name: 'Hangfire',
        publicMessage: '0 failed, 1 processing, 2 enqueued',
        isHealthy: true,
        children: [],
      }}
    />
  );

  expect(screen.getByText('0 failed, 1 processing, 2 enqueued')).toBeDefined();
});
