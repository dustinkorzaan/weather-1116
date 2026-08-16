import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { createMemoryRouter, MemoryRouter, RouterProvider } from 'react-router-dom';
import App, { AboutTreeNode } from './App';
import { MAP_CITIES_STORAGE_KEY } from './data/mapCities';
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

function mockHelloFetch(weather = {}) {
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

    if (url.includes('/Geo/GetLocation')) {
      return new Response(JSON.stringify({ location: 'Nashville, Tennessee' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    if (url.includes('/Geo')) {
      await new Promise((resolve) => {
        setTimeout(resolve, 40);
      });
      return new Response(
        JSON.stringify({
          rank: 1,
          name: 'Nashville',
          state: 'Tennessee',
          country: 'United States',
          latitude: 36.1627,
          longitude: -86.7816,
        }),
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
          windDirectionDegrees: 180,
          conditions: 'Clear',
          locationName: 'Nashville, TN',
          latitude: 36.1627,
          longitude: -86.7816,
          ...weather,
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
  document.documentElement.classList.remove('dark');
  document.documentElement.removeAttribute('data-theme');
  document.documentElement.removeAttribute('data-theme-preference');
  window.localStorage.removeItem('weather-theme');
  window.sessionStorage.removeItem(MAP_CITIES_STORAGE_KEY);
});

test('user menu is a gray outline control instead of a solid blue button', () => {
  mockHelloFetch();
  renderApp('/');

  const button = screen.getByRole('button', { name: /open user menu/i });
  expect(button.className).not.toMatch(/bg-blue/);
  expect(button.className).toMatch(/border-2/);
});

test('header person icon uses the shared filled avatar svg', () => {
  mockHelloFetch();
  renderApp('/');

  const title = screen.getByRole('heading', { name: /weather react/i });
  const avatar = title.closest('header')?.querySelector('img.avatar-icon');
  expect(avatar).toBeTruthy();
  expect(avatar?.getAttribute('src')).toBe('/avatar.svg');
  expect(avatar?.getAttribute('width')).toBe('20');
  expect(avatar?.getAttribute('height')).toBe('20');

  const avatarSvg = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), '../public/avatar.svg'),
    'utf8'
  );
  expect(avatarSvg).toContain('<path ');
  expect(avatarSvg).not.toContain('<circle ');
});

test('renders the map on the home route without split page content', () => {
  mockHelloFetch();
  renderApp('/');

  expect(screen.getByRole('region', { name: /map/i })).toBeDefined();
  expect(screen.getByRole('button', { name: /add location/i })).toBeDefined();
  expect(screen.queryByText('Hello from test API.')).toBeNull();
  expect(screen.queryByRole('heading', { name: /chat clients/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();

  const mapSource = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), 'components/WeatherMap.jsx'),
    'utf8'
  );
  expect(mapSource).toContain('bindRightClickAddLocation');
  expect(mapSource).toContain('cityFromReverseLookup');
  expect(mapSource).toContain('useLazyGetLocationQuery');
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
  expect(screen.queryByRole('button', { name: /add location/i })).toBeNull();
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
  expect(screen.queryByRole('button', { name: /add location/i })).toBeNull();
});

test('current AI weather submit is a charcoal button instead of blue or a flat outline', () => {
  mockHelloFetch();
  renderApp('/current-ai-weather');

  const button = screen.getByRole('button', { name: /get current ai weather/i });
  expect(button.className).toMatch(/bg-primary/);
  expect(button.className).toMatch(/text-primary-foreground/);
  expect(button.className).toMatch(/cursor-pointer/);
  expect(button.className).not.toMatch(/bg-blue/);
  expect(button.className).not.toMatch(/bg-white/);
});

test('chat tabs and send use clickable gray controls instead of blue', () => {
  mockHelloFetch();
  renderApp('/chat-clients');

  expect(screen.getByRole('textbox', { name: /message/i }).closest('form')?.className).toMatch(
    /items-start/
  );

  const tab = screen.getByRole('tab', { name: 'Chat1a' });
  expect(tab.className).toMatch(/cursor-pointer/);
  expect(tab.className).toMatch(/border-2/);
  expect(tab.className).toMatch(/bg-muted|bg-primary/);
  expect(tab.className).not.toMatch(/bg-blue/);

  const send = screen.getByRole('button', { name: /^send$/i });
  expect(send.className).toMatch(/bg-primary/);
  expect(send.className).toMatch(/text-primary-foreground/);
  expect(send.className).toMatch(/cursor-pointer/);
  expect(send.className).not.toMatch(/bg-blue/);
});

test('current AI weather reads location query, clears it, and fetches', async () => {
  const fetchMock = mockHelloFetch();
  const { router } = renderAppWithRouter('/current-ai-weather?location=nashville%20tn');

  expect(screen.getByLabelText(/location:/i).value).toBe('nashville tn');

  await waitFor(() => {
    expect(screen.getByRole('button', { name: /get current ai weather/i }).getAttribute('aria-busy')).toBe(
      'true'
    );
  });

  await waitFor(() => {
    const summary = screen.getByText('Sunny in Nashville.');
    expect(summary.tagName).toBe('P');
    expect(summary.closest('.chat-markdown')).toBeTruthy();
  });

  const weatherUrl = fetchMock.mock.calls
    .map(([input]) => requestUrl(input))
    .find((url) => url.includes('/AIWeather/Current'));

  expect(weatherUrl).toBeDefined();
  expect(weatherUrl).toContain('location=nashville');
  expect(router.state.location.pathname).toBe('/current-ai-weather');
  expect(router.state.location.search).toBe('');

  expect(screen.getByText('Temperature')).toBeDefined();
  expect(screen.getByText('72 °F')).toBeDefined();
  expect(screen.getByText('Wind Speed')).toBeDefined();
  expect(screen.getByText('5 mph')).toBeDefined();
  expect(screen.getByText('Wind Direction')).toBeDefined();
  expect(screen.getByText('S (180°)')).toBeDefined();
  expect(screen.getByText('Lat/Long')).toBeDefined();
  expect(screen.getByText('36.16° N, 86.78° W')).toBeDefined();
  expect(screen.queryByText('Temperature F')).toBeNull();
  expect(screen.queryByText('Wind Speed MPH')).toBeNull();
});

test('current AI weather renders the full summary as GitHub-flavored Markdown', async () => {
  mockHelloFetch({
    fullSummary: `**Sunny** in Nashville.

| Metric | Value |
| --- | --- |
| Temp | 72 |
`,
  });
  const user = userEvent.setup();
  renderApp('/current-ai-weather');

  await user.click(screen.getByRole('button', { name: /get current ai weather/i }));

  await waitFor(() => {
    expect(screen.getByText('Sunny').tagName).toBe('STRONG');
    expect(screen.getByRole('table')).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Metric' })).toBeDefined();
    expect(screen.getByRole('cell', { name: 'Temp' })).toBeDefined();
  });
});

test('renders chat clients on its own page', () => {
  mockHelloFetch();
  renderApp('/chat-clients');

  expect(screen.getByRole('heading', { name: /chat clients/i })).toBeDefined();
  expect(screen.getByRole('tab', { name: 'Chat1a' })).toBeDefined();
  expect(screen.getByText('In-process · Responses API · Like Foundry Console V3')).toBeDefined();
  expect(screen.queryByRole('heading', { name: /^hello world$/i })).toBeNull();
  expect(screen.queryByRole('heading', { name: /current ai weather/i })).toBeNull();
  expect(screen.queryByRole('region', { name: /map/i })).toBeNull();
  expect(screen.queryByRole('button', { name: /add location/i })).toBeNull();
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
  expect(screen.getByRole('menuitemradio', { name: 'Light' })).toBeDefined();
  expect(screen.getByRole('menuitemradio', { name: 'Dark' })).toBeDefined();
  expect(screen.getByRole('menuitemradio', { name: 'System' })).toBeDefined();
});

test('user menu can switch the document to the dark theme', async () => {
  mockHelloFetch();
  const user = userEvent.setup();
  renderApp('/');

  await user.click(screen.getByRole('button', { name: /open user menu/i }));
  await user.click(await screen.findByRole('menuitemradio', { name: 'Dark' }));

  expect(document.documentElement.classList.contains('dark')).toBe(true);
  expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  expect(window.localStorage.getItem('weather-theme')).toBe('dark');
});

test('user menu can switch the document back to the light theme', async () => {
  mockHelloFetch();
  const user = userEvent.setup();
  renderApp('/');

  await user.click(screen.getByRole('button', { name: /open user menu/i }));
  await user.click(await screen.findByRole('menuitemradio', { name: 'Dark' }));
  await user.click(screen.getByRole('button', { name: /open user menu/i }));
  await user.click(await screen.findByRole('menuitemradio', { name: 'Light' }));

  expect(document.documentElement.classList.contains('dark')).toBe(false);
  expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  expect(window.localStorage.getItem('weather-theme')).toBe('light');
  expect(screen.getByRole('region', { name: /map/i })).toBeDefined();
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

test('header plus control opens a location popdown and stays open while geo search runs', async () => {
  const fetchMock = mockHelloFetch();
  const user = userEvent.setup();
  renderApp('/');

  const addButton = screen.getByRole('button', { name: /add location/i });
  const avatar = screen.getByRole('button', { name: /open user menu/i });
  expect(addButton.compareDocumentPosition(avatar) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

  await user.click(addButton);
  const locationInput = screen.getByLabelText(/^location$/i);
  expect(locationInput.value).toBe('Nashville, TN');

  await user.click(screen.getByRole('button', { name: /add to map/i }));

  await waitFor(() => {
    expect(screen.getByRole('button', { name: /looking up location/i }).getAttribute('aria-busy')).toBe(
      'true'
    );
  });
  expect(locationInput).toBeDefined();

  await waitFor(() => {
    expect(screen.queryByRole('button', { name: /looking up location/i })).toBeNull();
  });

  const geoUrl = fetchMock.mock.calls
    .map(([input]) => requestUrl(input))
    .find((url) => url.includes('/Geo'));
  expect(geoUrl).toBeDefined();
  expect(geoUrl).toContain('location=Nashville');
  expect(JSON.parse(window.sessionStorage.getItem(MAP_CITIES_STORAGE_KEY)).at(-1).name).toBe(
    'Nashville, Tennessee'
  );
});
