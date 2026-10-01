// Acceptance tests for docs/specs/2026-10-01-home-chat5a-sidebar-gates.md (React):
// AC1, AC2, AC3, AC4, AC10. The Core gate criteria (AC5-AC9) are covered in
// core-dotnet/core.tests/Chat/Chat5GateScopeAcceptanceTests.cs.
import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import ChatPanel from './components/chat/ChatPanel';
import { weatherApi } from './services/weatherApi';

// Google Maps cannot run under jsdom; render the pin set as a list instead.
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

const GATE_LABELS = ['500 Char', 'Code Input', 'LLM Input', 'Sys Prompt', 'LLM Output'];

const DEFAULT_GATES = {
  enableMaxLengthGate: true,
  enableRuleInputGate: false,
  enableLlmInputGate: true,
  enableSystemPromptGuard: true,
  enableLlmOutputGate: false,
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
 * POST shifts the next scripted reply (`{ events }`, `{ fail: true }` or `{ throws: true }`).
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

function gateRow(container) {
  return within(container)
    .getAllByRole('checkbox')
    .map((checkbox) => {
      const label = checkbox.closest('label');
      return {
        label: (label?.textContent ?? '').trim(),
        title: label?.getAttribute('title') ?? checkbox.getAttribute('title') ?? '',
        checked: checkbox.checked,
      };
    });
}

function chatPathnames(spy) {
  return spy.mock.calls
    .map(([input]) => new URL(requestUrl(input), 'http://localhost').pathname)
    .filter((pathname) => /\/Chat\w+\/messages$/.test(pathname));
}

afterEach(() => {
  vi.restoreAllMocks();
});

// AC1
test('AC1: sends go once each to /Chat5a/messages with the default gate fields and the session id', async () => {
  const backend = mockBackend({
    chatReplies: [
      { events: [{ type: 'session', sessionId: 'sidebar-5a-1' }, { type: 'token', text: 'first' }, { type: 'done' }] },
      { events: [{ type: 'token', text: 'second' }, { type: 'done' }] },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'Add Nashville');
  await waitFor(() => expect(within(panel).getByText('first')).toBeDefined());

  expect(backend.chat5aPosts()).toBe(1);
  expect(backend.state.chatBodies[0]).toEqual({ sessionId: null, message: 'Add Nashville', ...DEFAULT_GATES });

  // The second send reuses the id from the stream's session event.
  await sendFromSidebar(user, panel, 'Remove Denver');
  await waitFor(() => expect(within(panel).getByText('second')).toBeDefined());

  expect(backend.chat5aPosts()).toBe(2);
  expect(backend.state.chatBodies[1]).toEqual({ sessionId: 'sidebar-5a-1', message: 'Remove Denver', ...DEFAULT_GATES });
});

// AC1 (negative)
test('AC1: the sidebar never posts to /Chat2a/messages or any chat endpoint other than Chat5a', async () => {
  const backend = mockBackend({ chatReplies: [{ events: [{ type: 'token', text: 'ok' }, { type: 'done' }] }] });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'hello');
  await waitFor(() => expect(within(panel).getByText('ok')).toBeDefined());

  const paths = chatPathnames(backend.spy);
  expect(paths.filter((p) => p.endsWith('/Chat2a/messages'))).toEqual([]);
  expect(paths.filter((p) => !p.endsWith('/Chat5a/messages'))).toEqual([]);
  expect(paths).toHaveLength(1);
});

// AC2
test('AC2: the panel is a complementary region named Chat5a with a Chat5a heading', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);

  expect(screen.getByRole('complementary', { name: 'Chat5a' })).toBe(panel);
  expect(within(panel).getByRole('heading', { name: 'Chat5a' })).toBeDefined();
  expect(screen.queryByRole('complementary', { name: 'Chat2a' })).toBeNull();
  expect(panel.id).toBe('chat5a-sidebar');
  expect(panel.querySelector('#chat5a-sidebar-input')).not.toBeNull();
  expect(panel.querySelector('[data-chat5a-sidebar-messages]')).not.toBeNull();
});

// AC2
test('AC2: five gate checkboxes sit below the textarea in order, Code Input and LLM Output unchecked, the rest checked', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  const textarea = within(panel).getByRole('textbox');
  const checkboxes = within(panel).getAllByRole('checkbox');

  expect(checkboxes).toHaveLength(5);
  const row = gateRow(panel);
  row.forEach((gate, index) => expect(gate.label.startsWith(GATE_LABELS[index])).toBe(true));
  expect(row.map((gate) => gate.checked)).toEqual([true, false, true, true, false]);

  for (const checkbox of checkboxes) {
    expect(textarea.compareDocumentPosition(checkbox) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  }

  expect(within(panel).getByRole('checkbox', { name: /^Code Input/ }).checked).toBe(false);
  expect(within(panel).getByRole('checkbox', { name: /^LLM Output/ }).checked).toBe(false);
  expect(within(panel).getByRole('checkbox', { name: /^500 Char/ }).checked).toBe(true);
});

// AC2: same labels and hover descriptions as the /chat-clients Chat5a tab.
test('AC2: gate labels and hover descriptions match the /chat-clients Chat5a tab', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  const sidebarRow = gateRow(panel).map(({ label, title }) => ({ label, title }));
  sidebarRow.forEach(({ title }) => expect(title).not.toBe(''));

  const { container } = render(<ChatPanel />);
  await user.click(within(container).getByRole('tab', { name: 'Chat5a' }));
  const panelRow = gateRow(container).map(({ label, title }) => ({ label, title }));

  expect(sidebarRow).toEqual(panelRow);
});

// AC2 (negative: the Code Input / LLM Output defaults change only in the sidebar)
test('AC2: the /chat-clients Chat5a tab still defaults all five gates to checked', async () => {
  mockBackend();
  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);
  await user.click(within(container).getByRole('tab', { name: 'Chat5a' }));

  expect(gateRow(container).map((gate) => gate.checked)).toEqual([true, true, true, true, true]);
});

// AC2: toggling changes the matching field on the next send.
test('AC2: toggling gates (check Code Input, check LLM Output, uncheck LLM Input) changes the matching fields on the next send', async () => {
  const backend = mockBackend({
    chatReplies: [
      { events: [{ type: 'token', text: 'one' }, { type: 'done' }] },
      { events: [{ type: 'token', text: 'two' }, { type: 'done' }] },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await user.click(within(panel).getByRole('checkbox', { name: /^Code Input/ }));
  await sendFromSidebar(user, panel, 'Save Denver');
  await waitFor(() => expect(within(panel).getByText('one')).toBeDefined());

  expect(backend.state.chatBodies[0]).toEqual({
    sessionId: null,
    message: 'Save Denver',
    ...DEFAULT_GATES,
    enableRuleInputGate: true,
  });

  await user.click(within(panel).getByRole('checkbox', { name: /^LLM Output/ }));
  await user.click(within(panel).getByRole('checkbox', { name: /^LLM Input/ }));
  await sendFromSidebar(user, panel, 'Pin Seattle');
  await waitFor(() => expect(within(panel).getByText('two')).toBeDefined());

  expect(backend.state.chatBodies[1]).toEqual({
    sessionId: null,
    message: 'Pin Seattle',
    ...DEFAULT_GATES,
    enableRuleInputGate: true,
    enableLlmOutputGate: true,
    enableLlmInputGate: false,
  });
});

// AC2: gate state survives close and reopen like the conversation does.
test('AC2: gate state survives closing and reopening the sidebar', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  let panel = await openSidebar(user);
  await user.click(within(panel).getByRole('checkbox', { name: /^Code Input/ }));
  await user.click(within(panel).getByRole('button', { name: 'Close chat' }));
  await waitFor(() => expect(visiblePanel()).toBeNull());

  panel = await openSidebar(user);
  expect(within(panel).getByRole('checkbox', { name: /^Code Input/ }).checked).toBe(true);
});

// AC3
test('AC3: a blocked event renders the message styled as a blocked entry, not an error', async () => {
  mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'blocked', errorMessage: 'Blocked by LLM Input: message is out of scope' },
          { type: 'done' },
        ],
      },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  await sendFromSidebar(user, panel, 'Tell me a joke.');

  const entry = await within(panel).findByText('Blocked by LLM Input: message is out of scope');
  expect(entry.className).toContain('amber');
  expect(entry.className).not.toContain('destructive');
});

// AC3: the other events keep working and the map still re-reads /User once per send.
test('AC3: token, tool and error events still render and each completed send re-reads GET /User once', async () => {
  const backend = mockBackend({
    chatReplies: [
      {
        events: [
          { type: 'session', sessionId: 's1' },
          { type: 'tool_start', toolName: 'AddUserCity' },
          { type: 'tool_end', toolName: 'AddUserCity', toolResult: '{}' },
          { type: 'token', text: 'Saved Nashville ' },
          { type: 'token', text: 'to your cities.' },
          { type: 'done' },
        ],
      },
      { events: [{ type: 'error', errorMessage: 'Chat5a failed upstream.' }, { type: 'done' }] },
      { events: [{ type: 'blocked', errorMessage: 'Blocked by 500 Char' }, { type: 'done' }] },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  await waitFor(() => expect(backend.userGets()).toBe(1));
  const panel = await openSidebar(user);

  await sendFromSidebar(user, panel, 'Add Nashville');
  await waitFor(() => expect(within(panel).getByText('Saved Nashville to your cities.')).toBeDefined());
  expect(within(panel).getAllByText(/AddUserCity/).length).toBeGreaterThan(0);
  await waitFor(() => expect(backend.userGets()).toBe(2));

  await sendFromSidebar(user, panel, 'again');
  const error = await within(panel).findByText('Chat5a failed upstream.');
  expect(error.className).toContain('destructive');
  await waitFor(() => expect(backend.userGets()).toBe(3));

  await sendFromSidebar(user, panel, 'blocked one');
  await within(panel).findByText('Blocked by 500 Char');
  await waitFor(() => expect(backend.userGets()).toBe(4));

  await new Promise((resolve) => setTimeout(resolve, 50));
  expect(backend.userGets()).toBe(4);
  expect(backend.chat5aPosts()).toBe(3);
});

// AC4
test('AC4: the Open chat button toggles the Chat5a panel and points aria-controls at it', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  const toggle = screen.getByRole('button', { name: 'Open chat' });
  expect(toggle.getAttribute('aria-expanded')).toBe('false');
  expect(toggle.getAttribute('aria-controls')).toBe('chat5a-sidebar');
  expect(visiblePanel()).toBeNull();

  const panel = await openSidebar(user);
  expect(toggle.getAttribute('aria-expanded')).toBe('true');
  expect(header().contains(panel)).toBe(false);

  await user.click(toggle);
  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(toggle.getAttribute('aria-expanded')).toBe('false');
});

// AC4
test('AC4: Close chat and Escape both close the Chat5a panel', async () => {
  mockBackend();
  const user = userEvent.setup();
  await renderApp('/');

  let panel = await openSidebar(user);
  await user.click(within(panel).getByRole('button', { name: 'Close chat' }));
  await waitFor(() => expect(visiblePanel()).toBeNull());

  panel = await openSidebar(user);
  await user.click(within(panel).getByRole('textbox'));
  await user.keyboard('{Escape}');
  await waitFor(() => expect(visiblePanel()).toBeNull());
  expect(screen.getByRole('button', { name: 'Open chat' }).getAttribute('aria-expanded')).toBe('false');
});

// AC4 (negative: the button exists only on the map page)
test.each(['/hello-world', '/chat-clients'])('AC4: %s has no Open chat button', async (path) => {
  mockBackend();
  await renderApp(path);

  expect(screen.queryByRole('button', { name: 'Open chat' })).toBeNull();
});

// AC10
test.each([
  ['ended normally', { events: [{ type: 'token', text: 'Saved.' }, { type: 'done' }] }],
  ['was blocked', { events: [{ type: 'blocked', errorMessage: 'Blocked by Code Input' }, { type: 'done' }] }],
  ['failed with an HTTP error', { fail: true }],
  ['failed with a network error', { throws: true }],
])('AC10: after a send that %s the textarea is enabled and focused', async (_label, reply) => {
  const backend = mockBackend({ chatReplies: [reply] });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  const textarea = within(panel).getByRole('textbox');
  await sendFromSidebar(user, panel, 'Add Nashville');

  // Clicking Send moved focus to the button; completion must bring it back.
  await waitFor(() => expect(backend.chat5aPosts()).toBe(1));
  await waitFor(() => expect(backend.userGets()).toBeGreaterThanOrEqual(2));
  await waitFor(() => {
    expect(textarea.disabled).toBe(false);
    expect(document.activeElement).toBe(textarea);
  });
});

// AC10: holds for every send, not just the first.
test('AC10: focus returns to the textarea after each of several sends', async () => {
  mockBackend({
    chatReplies: [
      { events: [{ type: 'token', text: 'r1' }, { type: 'done' }] },
      { events: [{ type: 'token', text: 'r2' }, { type: 'done' }] },
    ],
  });
  const user = userEvent.setup();
  await renderApp('/');

  const panel = await openSidebar(user);
  const textarea = within(panel).getByRole('textbox');

  await sendFromSidebar(user, panel, 'one');
  await within(panel).findByText('r1');
  await waitFor(() => expect(document.activeElement).toBe(textarea));

  await sendFromSidebar(user, panel, 'two');
  await within(panel).findByText('r2');
  await waitFor(() => expect(document.activeElement).toBe(textarea));
});
