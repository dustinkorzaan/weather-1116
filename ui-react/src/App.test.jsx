import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { createMemoryRouter, MemoryRouter, RouterProvider } from 'react-router-dom';
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

function requestUrl(input) {
  return input instanceof Request ? input.url : String(input);
}

function mockHelloFetch() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = requestUrl(input);

    if (url.includes('/Home/Hello')) {
      return new Response(
        JSON.stringify({ requestResponse: 'Hello from test API.' }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }
      );
    }

    if (url.includes('/AIWeather/Current')) {
      await new Promise((resolve) => {
        setTimeout(resolve, 40);
      });
      return new Response(
        JSON.stringify({
          fullSummary: 'Sunny in Nashville.',
          temperatureF: 72,
          windSpeedMPH: 5,
          windDirection: 'S',
          conditions: 'Clear',
        }),
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

function renderAppWithRouter(path) {
  const store = createTestStore();
  const router = createMemoryRouter([{ path: '*', element: <App /> }], {
    initialEntries: [path],
  });

  const view = render(
    <Provider store={store}>
      <RouterProvider router={router} />
    </Provider>
  );

  return { ...view, router };
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

test('renders the map on the home route without split page content', () => {
  mockHelloFetch();
  renderApp('/');

  expect(screen.getByRole('region', { name: /map/i })).toBeDefined();
  expect(screen.queryByText('Hello from test API.')).toBeNull();
  expect(screen.queryByRole('heading', { name: /chat clients/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();
});

test('renders hello world on its own page', async () => {
  mockHelloFetch();
  renderApp('/hello-world');

  expect(await screen.findByRole('heading', { name: /weather react/i })).toBeDefined();
  expect(await screen.findByRole('heading', { name: /^hello world$/i })).toBeDefined();
  expect(await screen.findByText('Hello from test API.')).toBeDefined();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /chat clients/i })).toBeNull();
  expect(screen.queryByRole('region', { name: /map/i })).toBeNull();
});

test('renders current AI weather on its own page', () => {
  mockHelloFetch();
  renderApp('/current-ai-weather');

  expect(screen.getByRole('heading', { name: /current ai weather/i })).toBeDefined();
  expect(screen.getByLabelText(/location:/i)).toBeDefined();
  expect(screen.getByRole('button', { name: /get current ai weather/i })).toBeDefined();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /chat clients/i })).toBeNull();
  expect(screen.queryByRole('region', { name: /map/i })).toBeNull();
});

test('current AI weather reads location query, clears it, and fetches', async () => {
  const fetchMock = mockHelloFetch();
  const { router } = renderAppWithRouter('/current-ai-weather?location=nashville%20tn');

  expect(screen.getByLabelText(/location:/i).value).toBe('nashville, TN');

  await waitFor(() => {
    expect(screen.getByRole('button', { name: /get current ai weather/i }).getAttribute('aria-busy')).toBe(
      'true'
    );
  });

  await waitFor(() => {
    expect(screen.getByText('Sunny in Nashville.')).toBeDefined();
  });

  const weatherUrl = fetchMock.mock.calls
    .map(([input]) => requestUrl(input))
    .find((url) => url.includes('/AIWeather/Current'));

  expect(weatherUrl).toBeDefined();
  expect(weatherUrl).toContain('location=nashville');
  expect(router.state.location.pathname).toBe('/current-ai-weather');
  expect(router.state.location.search).toBe('');
});

test('renders chat clients on its own page', () => {
  mockHelloFetch();
  renderApp('/chat-clients');

  expect(screen.getByRole('heading', { name: /chat clients/i })).toBeDefined();
  expect(screen.getByRole('tab', { name: 'Chat1a' })).toBeDefined();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();
  expect(screen.queryByRole('region', { name: /map/i })).toBeNull();
});

test('user menu lists login and the three content pages', async () => {
  mockHelloFetch();
  const user = userEvent.setup();
  renderApp('/');

  await user.click(screen.getByRole('button', { name: /open user menu/i }));

  expect(await screen.findByRole('menuitem', { name: 'Login/Logout' })).toBeDefined();
  expect(screen.getByRole('menuitem', { name: 'Hello World' })).toBeDefined();
  expect(screen.getByRole('menuitem', { name: 'Current AI Weather' })).toBeDefined();
  expect(screen.getByRole('menuitem', { name: 'Chat Clients' })).toBeDefined();
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
