import { afterEach, expect, test, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import HelloWorldPage from './HelloWorldPage';
import { weatherApi } from '../services/weatherApi';

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

test('renders Hello World heading and hello message', async () => {
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

    return new Response(JSON.stringify({}), {
      status: 404,
      headers: { 'Content-Type': 'application/json' },
    });
  });

  render(
    <Provider store={store}>
      <MemoryRouter>
        <HelloWorldPage />
      </MemoryRouter>
    </Provider>
  );

  expect(await screen.findByRole('heading', { name: /hello world/i })).toBeDefined();
  expect(await screen.findByText('Hello from test API.')).toBeDefined();
});

test('shows error message when API call fails', async () => {
  const store = createTestStore();

  vi.spyOn(globalThis, 'fetch').mockImplementation(async () => {
    return new Response(JSON.stringify({}), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  });

  render(
    <Provider store={store}>
      <MemoryRouter>
        <HelloWorldPage />
      </MemoryRouter>
    </Provider>
  );

  expect(await screen.findByText('Unable to load hello message from API.')).toBeDefined();
});
