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

function renderApp(path) {
  const store = createTestStore();

  return render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </Provider>
  );
}

function mockHelloFetch() {
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
}

afterEach(() => {
  vi.restoreAllMocks();
});

test('user menu is a gray outline control instead of a solid blue button', () => {
  mockHelloFetch();
  renderApp('/');

  const button = screen.getByRole('button', { name: /open user menu/i });
  expect(button.className).not.toMatch(/bg-blue/);
  expect(button.className).toMatch(/border-2/);
});

test('header person icon uses a bolder stroke', () => {
  mockHelloFetch();
  renderApp('/');

  const title = screen.getByRole('heading', { name: /weather react/i });
  expect(title.closest('header')?.querySelector('svg')?.getAttribute('stroke-width')).toBe('2.25');
});

test('renders the map on the home route without presentation content', () => {
  mockHelloFetch();
  renderApp('/');

  expect(screen.getByRole('region', { name: /map/i })).toBeDefined();
  expect(screen.queryByText('Hello from test API.')).toBeNull();
  expect(screen.queryByRole('heading', { name: /chat clients/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();
});

test('renders weather app title and loaded data on the presentation page', async () => {
  mockHelloFetch();
  renderApp('/presentation');

  expect(await screen.findByRole('heading', { name: /weather react/i })).toBeDefined();
  expect(await screen.findByRole('heading', { name: /^hello world$/i })).toBeDefined();
  expect(await screen.findByText('Hello from test API.')).toBeDefined();
  expect(await screen.findByRole('heading', { name: /current ai weather/i })).toBeDefined();
  expect(await screen.findByLabelText(/location:/i)).toBeDefined();
  expect(await screen.findByRole('button', { name: /get current ai weather/i })).toBeDefined();
  expect(await screen.findByRole('heading', { name: /chat clients/i })).toBeDefined();
  expect(screen.getByRole('tab', { name: 'Chat1a' })).toBeDefined();
  expect(screen.queryByRole('region', { name: /map/i })).toBeNull();
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
