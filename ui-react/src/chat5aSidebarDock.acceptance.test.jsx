// Acceptance tests for docs/specs/2026-09-30-chat2a-sidebar.md (React), carried over to the
// Chat5a sidebar by docs/specs/2026-10-01-home-chat5a-sidebar-gates.md.
import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import chat5aSidebarSource from './components/chat/Chat5aSidebar.jsx?raw';
import { weatherApi } from './services/weatherApi';

// Google Maps cannot run under jsdom. The real WeatherMap paints one marker per
// `useMapPins().cities` entry, so this probe renders that same pin set as a list.
vi.mock('./components/WeatherMap', async () => {
  const { useMapPins } = await import('./map/mapPinsContext');
  function MapPinsProbe() {
    const { cities } = useMapPins();
    return (
      <section aria-label="Map">
        <ul data-testid="map-pins">
          {cities.map((city) => (
            <li key={city.id}>{city.locationName}</li>
          ))}
        </ul>
      </section>
    );
  }
  return { default: MapPinsProbe };
});

const NASHVILLE = {
  id: '11111111-1111-1111-1111-111111111111',
  latitude: 36.1627,
  longitude: -86.7816,
  locationName: 'Nashville, Tennessee',
};

const DENVER = {
  id: '22222222-2222-2222-2222-222222222222',
  latitude: 39.7392,
  longitude: -104.9903,
  locationName: 'Denver, Colorado',
};

function createTestStore() {
  return configureStore({
    reducer: {
      [weatherApi.reducerPath]: weatherApi.reducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(weatherApi.middleware),
  });
}

async function renderApp(path) {
  const store = createTestStore();

  const view = render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </Provider>
  );

  await waitFor(() => {
    expect(screen.queryByTestId('backend-wake-screen')).toBeNull();
  });

  return view;
}

function requestUrl(input) {
  return input instanceof Request ? input.url : String(input);
}

function requestMethod(input, init) {
  return (init?.method ?? (input instanceof Request ? input.method : 'GET')).toUpperCase();
}

function sseResponse(events) {
  const text = events.map((event) => `data: ${JSON.stringify(event)}\n\n`).join('');
  const stream = new ReadableStream({
    start(controller) {
      controller.enqueue(new TextEncoder().encode(text));
      controller.close();
    },
  });
  return new Response(stream, { status: 200, headers: { 'Content-Type': 'text/event-stream' } });
}

function jsonResponse(body, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

/**
 * Stateful backend: `/User` returns `state.userCities`; each `/Chat5a/messages`
 * POST shifts the next scripted reply (`{ events, userCitiesAfter }`, `{ fail: true }` or `{ throws: true }`).
 */
function mockBackend({ userCities = [], chatReplies = [] } = {}) {
  const state = { userCities, chatReplies: [...chatReplies], chatBodies: [] };

  const spy = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    const url = requestUrl(input);
    const pathname = new URL(url, 'http://localhost').pathname;

    if (pathname.endsWith('/Chat5a/messages')) {
      const rawBody = init?.body ?? (input instanceof Request ? await input.clone().text() : undefined);
      state.chatBodies.push(rawBody === undefined ? undefined : JSON.parse(rawBody));
      const reply = state.chatReplies.shift() ?? { events: [{ type: 'done' }] };
      if (reply.userCitiesAfter) {
        state.userCities = reply.userCitiesAfter;
      }
      if (reply.response) {
        return reply.response();
      }
      if (reply.throws) {
        throw new TypeError('Failed to fetch');
      }
      if (reply.fail) {
        return jsonResponse({ error: 'boom' }, 500);
      }
      return sseResponse(reply.events);
    }

    if (url.includes('/About')) {
      return jsonResponse({ name: 'API Root', isHealthy: true, children: [] });
    }

    if (url.includes('/Home/Hello')) {
      return jsonResponse({ requestResponse: 'Hello from test API.' });
    }

    if (pathname.endsWith('/User')) {
      return jsonResponse({
        id: '00000000-0000-0000-0000-000000000000',
        firstName: '',
        lastName: '',
        email: '',
        userCities: state.userCities,
      });
    }

    return jsonResponse({}, 404);
  });

  const chat5aPosts = () =>
    spy.mock.calls.filter(
      ([input, init]) =>
        new URL(requestUrl(input), 'http://localhost').pathname.endsWith('/Chat5a/messages') &&
        requestMethod(input, init) === 'POST'
    ).length;

  const userGets = () =>
    spy.mock.calls.filter(
      ([input, init]) =>
        new URL(requestUrl(input), 'http://localhost').pathname.endsWith('/User') &&
        requestMethod(input, init) === 'GET'
    ).length;

  return { spy, state, chat5aPosts, userGets };
}

function header() {
  return screen.getByRole('heading', { name: /weather react/i }).closest('header');
}

function headerButtonNames() {
  return within(header())
    .getAllByRole('button')
    .map((button) => button.getAttribute('aria-label') ?? button.textContent.trim());
}

function visiblePanel() {
  return (
    screen.queryByRole('complementary', { name: 'Chat5a' }) ??
    screen.queryByRole('dialog', { name: 'Chat5a' })
  );
}

async function openSidebar(user) {
  await user.click(screen.getByRole('button', { name: 'Open chat' }));
  await waitFor(() => expect(visiblePanel()).not.toBeNull());
  return visiblePanel();
}

async function sendFromSidebar(user, panel, message) {
  await user.type(within(panel).getByRole('textbox'), message);
  await user.click(within(panel).getByRole('button', { name: /^send$/i }));
}

afterEach(() => {
  vi.restoreAllMocks();
});

// AC1
test('AC1: home header actions are Add location, then Open chat, then the avatar', async () => {
  mockBackend();
  await renderApp('/');

  expect(screen.getByRole('button', { name: 'Open chat' })).toBeDefined();
  expect(headerButtonNames()).toEqual(['Add location', 'Open chat', 'Open user menu']);
});

// AC1 (negative: the chat button exists only on the map page)
test.each(['/hello-world', '/current-ai-weather', '/chat-clients'])(
  'AC1: %s has no Open chat button in the header',
  async (path) => {
    mockBackend();
    await renderApp(path);

    expect(screen.queryByRole('button', { name: 'Open chat' })).toBeNull();
    expect(headerButtonNames()).toEqual(['Open user menu']);
  }
);

// AC2
test('AC2: Open chat reveals a Chat5a panel and reports aria-expanded', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const toggle = screen.getByRole('button', { name: 'Open chat' });
  expect(toggle.getAttribute('aria-expanded')).toBe('false');
  expect(visiblePanel()).toBeNull();

  const panel = await openSidebar(user);
  expect(toggle.getAttribute('aria-expanded')).toBe('true');
  expect(screen.getByRole('complementary', { name: 'Chat5a' })).toBe(panel);

  // The panel is outside the header (it sits below the top bar, not inside it).
  expect(header().contains(panel)).toBe(false);
});

function commonAncestor(a, b) {
  let node = a.parentElement;
  while (node && !node.contains(b)) {
    node = node.parentElement;
  }
  return node;
}

// AC2 (docked beside the map, not a fixed overlay)
test('AC2: the open panel is docked in a row with the map (stacked below 640px), not a fixed overlay', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  const map = screen.getByRole('region', { name: /map/i });

  // No fixed/absolute overlay on the panel itself.
  expect(panel.className).not.toMatch(/(^|\s)(fixed|absolute)(\s|$)/);
  expect(panel.style.position).not.toMatch(/fixed|absolute/);

  // Map first, then the panel, in the same flex container that is a column
  // below the sm (640px) breakpoint and a row at >= 640px.
  expect(map.compareDocumentPosition(panel) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  const row = commonAncestor(map, panel);
  expect(row).not.toBeNull();
  expect(row.className).toMatch(/(^|\s)flex(\s|$)/);
  expect(row.className).toMatch(/(^|\s)flex-col(\s|$)/);
  expect(row.className).toMatch(/(^|\s)sm:flex-row(\s|$)/);
  expect(header().contains(row)).toBe(false);

  // Every ancestor between the panel and the row is in-flow as well.
  for (let node = panel; node && node !== row; node = node.parentElement) {
    expect(node.className).not.toMatch(/(^|\s)fixed(\s|$)/);
  }
});

// AC2 (leaving / hides the panel)
test('AC2: navigating away from / hides the open panel', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  await openSidebar(user);

  await user.click(screen.getByRole('button', { name: /open user menu/i }));
  await user.click(await screen.findByRole('menuitem', { name: /hello world/i }));

  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(screen.queryByRole('button', { name: 'Open chat' })).toBeNull();
});

// AC2 (toggle closes)
test('AC2: clicking Open chat again hides the panel', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  await openSidebar(user);
  const toggle = screen.getByRole('button', { name: 'Open chat' });
  await user.click(toggle);

  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(toggle.getAttribute('aria-expanded')).toBe('false');
});

// AC2 (Close chat button)
test('AC2: the panel Close chat button hides the panel', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await user.click(within(panel).getByRole('button', { name: 'Close chat' }));

  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(screen.getByRole('button', { name: 'Open chat' }).getAttribute('aria-expanded')).toBe('false');
});

// AC2 (Escape, per spec Assumptions)
test('AC2: Escape closes the panel', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await user.click(within(panel).getByRole('textbox'));
  await user.keyboard('{Escape}');

  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(screen.getByRole('button', { name: 'Open chat' }).getAttribute('aria-expanded')).toBe('false');
});

// AC3
test('AC3: a send posts { sessionId, message, gates } once to /Chat5a/messages and renders streamed events', async () => {
  const backend = mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'session', sessionId: 'sidebar-session-1' },
          { type: 'tool_start', toolName: 'AddUserCity', toolArguments: '{"location":"Nashville"}' },
          { type: 'tool_end', toolName: 'AddUserCity', toolResult: '{"ok":true}' },
          { type: 'token', text: 'Added ' },
          { type: 'token', text: 'Nashville to your cities.' },
          { type: 'done' },
        ],
      },
      {
        events: [
          { type: 'error', errorMessage: 'Chat5a failed upstream.' },
          { type: 'done' },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'add Nashville to my cities');

  await waitFor(() => {
    expect(within(panel).getByText('Added Nashville to your cities.')).toBeDefined();
  });
  expect(within(panel).getAllByText(/AddUserCity/).length).toBeGreaterThan(0);
  expect(within(panel).getByText('add Nashville to my cities')).toBeDefined();

  expect(backend.chat5aPosts()).toBe(1);
  expect(backend.state.chatBodies[0]).toEqual({
    sessionId: null,
    message: 'add Nashville to my cities',
    enableMaxLengthGate: true,
    enableRuleInputGate: false,
    enableLlmInputGate: true,
    enableSystemPromptGuard: true,
    enableLlmOutputGate: true,
  });

  // Second send reuses the session id from the stream's `session` event.
  await sendFromSidebar(user, panel, 'what else?');
  await waitFor(() => {
    expect(within(panel).getByText('Chat5a failed upstream.')).toBeDefined();
  });

  expect(backend.chat5aPosts()).toBe(2);
  expect(backend.state.chatBodies[1]).toEqual({
    sessionId: 'sidebar-session-1',
    message: 'what else?',
    enableMaxLengthGate: true,
    enableRuleInputGate: false,
    enableLlmInputGate: true,
    enableSystemPromptGuard: true,
    enableLlmOutputGate: true,
  });
});

// AC3 (edge: the sidebar never calls another chat endpoint)
test('AC3: the sidebar never posts to a chat endpoint other than Chat5a', async () => {
  const backend = mockBackend({ chatReplies: [{ events: [{ type: 'token', text: 'ok' }, { type: 'done' }] }] });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'hello');
  await waitFor(() => expect(within(panel).getByText('ok')).toBeDefined());

  const otherChatPosts = backend.spy.mock.calls
    .map(([input]) => new URL(requestUrl(input), 'http://localhost').pathname)
    .filter((pathname) => /\/Chat\w+\/messages$/.test(pathname) && !pathname.endsWith('/Chat5a/messages'));
  expect(otherChatPosts).toEqual([]);
});

// AC4
test('AC4: every completed send, including a failed one, triggers exactly one GET /User', async () => {
  const backend = mockBackend({
    chatReplies: [
      { events: [{ type: 'token', text: 'first reply' }, { type: 'done' }] },
      { fail: true },
      { events: [{ type: 'token', text: 'third reply' }, { type: 'done' }] },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  await waitFor(() => expect(backend.userGets()).toBe(1));

  const panel = await openSidebar(user);
  // Opening the sidebar alone must not refetch.
  expect(backend.userGets()).toBe(1);

  await sendFromSidebar(user, panel, 'one');
  await waitFor(() => expect(within(panel).getByText('first reply')).toBeDefined());
  await waitFor(() => expect(backend.userGets()).toBe(2));

  await sendFromSidebar(user, panel, 'two');
  await waitFor(() => expect(backend.userGets()).toBe(3));

  await sendFromSidebar(user, panel, 'three');
  await waitFor(() => expect(within(panel).getByText('third reply')).toBeDefined());
  await waitFor(() => expect(backend.userGets()).toBe(4));

  // No extra refetches trail behind: N sends -> N refetches.
  await new Promise((resolve) => setTimeout(resolve, 50));
  expect(backend.userGets()).toBe(4);
  expect(backend.chat5aPosts()).toBe(3);
});

// AC4 (edge: a network failure still refetches exactly once)
test('AC4: a send whose request throws still triggers exactly one GET /User', async () => {
  const backend = mockBackend({ chatReplies: [{ throws: true }] });
  const user = userEvent.setup();
  await renderApp('/');

  await waitFor(() => expect(backend.userGets()).toBe(1));

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'hi');

  await waitFor(() => expect(backend.userGets()).toBe(2));
  await new Promise((resolve) => setTimeout(resolve, 50));
  expect(backend.userGets()).toBe(2);
});

// AC5
test('AC5: the map pin set follows the refetched /User after a sidebar send (add, then remove)', async () => {
  const backend = mockBackend({
    userCities: [DENVER],
    chatReplies: [
      {
        userCitiesAfter: [DENVER, NASHVILLE],
        events: [
          { type: 'tool_start', toolName: 'AddUserCity' },
          { type: 'tool_end', toolName: 'AddUserCity', toolResult: '{}' },
          { type: 'token', text: 'Added Nashville.' },
          { type: 'done' },
        ],
      },
      {
        userCitiesAfter: [NASHVILLE],
        events: [
          { type: 'tool_start', toolName: 'DeleteUserCity' },
          { type: 'tool_end', toolName: 'DeleteUserCity', toolResult: '{}' },
          { type: 'token', text: 'Removed Denver.' },
          { type: 'done' },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const pinNames = () =>
    within(screen.getByTestId('map-pins'))
      .queryAllByRole('listitem')
      .map((item) => item.textContent)
      .sort();

  await waitFor(() => expect(pinNames()).toEqual(['Denver, Colorado']));

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'add Nashville');
  await waitFor(() => expect(pinNames()).toEqual(['Denver, Colorado', 'Nashville, Tennessee']));

  await sendFromSidebar(user, panel, 'remove Denver');
  await waitFor(() => expect(pinNames()).toEqual(['Nashville, Tennessee']));

  // Still on Home, no navigation happened; the map section is the same page.
  expect(screen.getByRole('region', { name: /map/i })).toBeDefined();
  expect(backend.userGets()).toBeGreaterThanOrEqual(3);
});

/** SSE response whose events are pushed by the test; `close()` ends the stream. */
function controlledSse() {
  let controllerRef;
  const stream = new ReadableStream({
    start(controller) {
      controllerRef = controller;
    },
  });
  return {
    response: () =>
      new Response(stream, { status: 200, headers: { 'Content-Type': 'text/event-stream' } }),
    push(event) {
      controllerRef.enqueue(new TextEncoder().encode(`data: ${JSON.stringify(event)}\n\n`));
    },
    close() {
      controllerRef.close();
    },
  };
}

// AC6 (markdown)
test('AC6: a finished sidebar reply renders as GFM markdown like /chat-clients', async () => {
  mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'token', text: '**Warmest**\n\n| City | Temp |\n| --- | --- |\n| Nashville | 72 |\n' },
          { type: 'done' },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'compare');

  await waitFor(() => expect(within(panel).getByRole('table')).toBeDefined());
  const strong = within(panel).getByText('Warmest');
  expect(strong.tagName).toBe('STRONG');
  expect(strong.closest('.chat-markdown')).not.toBeNull();
  expect(within(panel).queryByText(/\*\*Warmest\*\*/)).toBeNull();
});

// AC6 (markdown is sanitized)
test('AC6: sidebar markdown is sanitized (no raw HTML or script elements)', async () => {
  mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'token', text: 'Safe <img src=x onerror="alert(1)"> text <script>alert(2)</script>' },
          { type: 'done' },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'hi');

  await waitFor(() => expect(panel.querySelector('.chat-markdown')).not.toBeNull());
  expect(panel.querySelector('script')).toBeNull();
  expect(panel.querySelector('img[onerror]')).toBeNull();
});

// AC6 (usage chip)
test('AC6: a finished reply with usage shows the same usage chip and hover details as /chat-clients', async () => {
  mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'token', text: 'Nashville looks clear.' },
          {
            type: 'done',
            usage: {
              runtimeMs: 1240,
              inputTokenCount: 3100,
              cachedTokenCount: 200,
              outputTokenCount: 1118,
              reasoningTokenCount: 40,
              totalTokenCount: 4218,
            },
          },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'weather');

  const chip = await within(panel).findByText('1.24s · 4,218 tok');
  expect(chip.getAttribute('data-tool-details')).toContain('Total: 4,218');

  await user.hover(chip);
  await waitFor(() => {
    const tooltip = screen.getByRole('tooltip');
    expect(tooltip.textContent).toContain('Runtime: 1,240 ms');
    expect(tooltip.textContent).toContain('Input: 3,100');
    expect(tooltip.textContent).toContain('Total: 4,218');
  });
});

// AC6 (usage chip: negative)
test('AC6: a finished reply without usage shows no usage chip', async () => {
  mockBackend({ chatReplies: [{ events: [{ type: 'token', text: 'No usage here.' }, { type: 'done' }] }] });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'hi');

  await waitFor(() => expect(within(panel).getByText('No usage here.')).toBeDefined());
  expect(within(panel).queryByText(/\btok$/)).toBeNull();
});

// AC6 (tool hover card)
test('AC6: tool lines show Waiting for tool output while running, then Arguments and Result on hover', async () => {
  const sse = controlledSse();
  mockBackend({ chatReplies: [{ response: sse.response }] });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'add Nashville');

  sse.push({ type: 'tool_start', toolName: 'AddUserCity' });
  const running = await within(panel).findByText('Running AddUserCity …');
  await user.hover(running);
  await waitFor(() => expect(screen.getByRole('tooltip').textContent).toContain('Waiting for tool output…'));
  await user.unhover(running);
  await waitFor(() => expect(screen.queryByRole('tooltip')).toBeNull());

  sse.push({
    type: 'tool_end',
    toolName: 'AddUserCity',
    toolArguments: '{\n  "location": "Nashville, TN"\n}',
    toolResult: '{\n  "ok": true\n}',
  });
  sse.push({ type: 'token', text: 'Added.' });
  sse.push({ type: 'done' });
  sse.close();

  const finished = await within(panel).findByText('Ran AddUserCity …');
  await user.hover(finished);
  await waitFor(() => {
    const text = screen.getByRole('tooltip').textContent;
    expect(text).toContain('Arguments');
    expect(text).toContain('"location": "Nashville, TN"');
    expect(text).toContain('Result');
    expect(text).toContain('"ok": true');
  });
});

// AC6 (same shared rendering code, no second copy of the formatting logic)
test('AC6: the sidebar does not carry its own copy of the chat formatting helpers', () => {
  expect(chat5aSidebarSource).not.toMatch(/function\s+ToolChip\b/);
  expect(chat5aSidebarSource).not.toMatch(/role=["']tooltip["']/);
  expect(chat5aSidebarSource).not.toContain('Waiting for tool output');
  expect(chat5aSidebarSource).not.toMatch(/function\s+formatChatUsage/);
  expect(chat5aSidebarSource).not.toMatch(/\btok`/);
  expect(chat5aSidebarSource).not.toMatch(/Arguments\\n/);
});
