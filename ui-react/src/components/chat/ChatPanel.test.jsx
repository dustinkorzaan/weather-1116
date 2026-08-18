import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ChatPanel from './ChatPanel';
import { streamChatMessage } from '../../utils/chatStream';

vi.mock('../../utils/chatStream', () => ({
  streamChatMessage: vi.fn(),
}));

afterEach(() => {
  vi.restoreAllMocks();
  vi.resetAllMocks();
  vi.useRealTimers();
});

function stubChatMessagesScrollHeight(height) {
  Object.defineProperty(HTMLElement.prototype, 'scrollHeight', {
    configurable: true,
    get() {
      return this.hasAttribute('data-chat-messages') ? height : 0;
    },
  });
}

test('scrolls the visible chat to the bottom when a turn completes', async () => {
  stubChatMessagesScrollHeight(800);
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'token', text: 'Assistant finished this turn.' });
    onEvent({ type: 'done' });
  });

  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);
  const messages = container.querySelector('[data-chat-messages]');
  messages.scrollTop = 0;

  await user.type(screen.getByLabelText(/message/i), 'hello');
  await user.click(screen.getByRole('button', { name: /^send$/i }));

  await waitFor(() => {
    expect(screen.getByText('Assistant finished this turn.')).toBeDefined();
  });
  await waitFor(() => {
    expect(container.querySelector('[data-chat-messages]').scrollTop).toBe(800);
  });
});

test('scrolls to the bottom when switching among the five chats', async () => {
  stubChatMessagesScrollHeight(640);
  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);

  for (const name of ['Chat1b', 'Chat2a', 'Chat2b', 'Chat3', 'Chat1a']) {
    await user.click(screen.getByRole('tab', { name }));
    expect(container.querySelector('[data-chat-messages]').scrollTop).toBe(640);
  }
});

test('shows tool arguments and result on hover', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({
      type: 'tool_start',
      toolName: 'GetLatLong',
      toolArguments: '{\n  "location": "Nashville, TN"\n}',
    });
    onEvent({
      type: 'tool_end',
      toolName: 'GetLatLong',
      toolArguments: '{\n  "location": "Nashville, TN"\n}',
      toolResult: '[\n  {\n    "name": "Nashville"\n  }\n]',
    });
    onEvent({ type: 'token', text: 'Nashville looks clear.' });
    onEvent({ type: 'done' });
  });

  const user = userEvent.setup();
  render(<ChatPanel />);

  await user.type(screen.getByLabelText(/message/i), 'weather in nashville');
  await user.click(screen.getByRole('button', { name: /^send$/i }));

  const chip = await screen.findByText('Ran GetLatLong …');
  await user.hover(chip);

  await waitFor(() => {
    expect(screen.getByRole('tooltip').textContent).toContain('Arguments');
    expect(screen.getByRole('tooltip').textContent).toContain('Nashville, TN');
    expect(screen.getByRole('tooltip').textContent).toContain('Result');
    expect(screen.getByRole('tooltip').textContent).toContain('"name": "Nashville"');
  });
});

async function renderFinishedToolChip(user) {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({
      type: 'tool_start',
      toolName: 'GetPublicWeatherCurrent',
      toolArguments: '{\n  "latitude": 43.70643,\n  "longitude": -79.39864\n}',
    });
    onEvent({
      type: 'tool_end',
      toolName: 'GetPublicWeatherCurrent',
      toolArguments: '{\n  "latitude": 43.70643,\n  "longitude": -79.39864\n}',
      toolResult: '{\n  "timezone": "America/Toronto",\n  "elevation": 113\n}',
    });
    onEvent({ type: 'done' });
  });

  render(<ChatPanel />);
  await user.type(screen.getByLabelText(/message/i), 'weather in toronto');
  await user.click(screen.getByRole('button', { name: /^send$/i }));
  return screen.findByText('Ran GetPublicWeatherCurrent …');
}

test('keeps tool details open and scrollable when the pointer moves onto the popup', async () => {
  const user = userEvent.setup();
  const chip = await renderFinishedToolChip(user);

  await user.hover(chip);
  const tooltip = await screen.findByRole('tooltip');
  const body = tooltip.querySelector('pre');
  expect(body?.className).toContain('overflow-auto');
  expect(tooltip.className).not.toContain('pointer-events-none');
  expect(body?.className).not.toContain('pointer-events-none');

  await user.hover(tooltip);
  expect(screen.getByRole('tooltip')).toBeDefined();
  expect(body?.textContent).toContain('"elevation": 113');
});

test('chat window has a fullscreen control that expands the conversation', async () => {
  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);

  const button = screen.getByRole('button', { name: /enter fullscreen/i });
  expect(button).toBeDefined();

  await user.click(button);

  expect(container.querySelector('.chat-window')?.classList.contains('is-css-fullscreen')).toBe(true);
  expect(screen.getByRole('button', { name: /exit fullscreen/i })).toBeDefined();

  await user.click(screen.getByRole('button', { name: /exit fullscreen/i }));
  expect(container.querySelector('.chat-window')?.classList.contains('is-css-fullscreen')).toBe(false);
  expect(screen.getByRole('button', { name: /enter fullscreen/i })).toBeDefined();
});

test('does not close tool details until the pointer leaves the popup', async () => {
  const user = userEvent.setup();
  const chip = await renderFinishedToolChip(user);

  await user.hover(chip);
  const tooltip = await screen.findByRole('tooltip');
  await user.hover(tooltip);
  expect(screen.getByRole('tooltip')).toBeDefined();

  await user.unhover(tooltip);
  expect(screen.getByRole('tooltip')).toBeDefined();

  await waitFor(() => {
    expect(screen.queryByRole('tooltip')).toBeNull();
  });
});

test('assistant replies grow with their text instead of shrinking inside the transcript', async () => {
  const reply = Array.from({ length: 12 }, (_, index) => `Line ${index + 1} of the forecast.`).join('\n');
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'tool_end', toolName: 'GetPublicWeatherCurrent' });
    onEvent({ type: 'token', text: reply });
    onEvent({ type: 'done' });
  });

  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);

  await user.type(screen.getByLabelText(/message/i), 'weather in nashville');
  await user.click(screen.getByRole('button', { name: /^send$/i }));

  const bubble = await waitFor(() => {
    const node = screen.getByText('Line 1 of the forecast.', { exact: false }).closest('div');
    expect(node).toBeTruthy();
    return node;
  });

  expect(bubble.className).toContain('shrink-0');
  expect(bubble.className).toContain('h-max');
  expect(bubble.className).toContain('min-h-min');
  expect(bubble.className).toContain('overflow-visible');
  expect(bubble.className).not.toMatch(/\bmax-h-/);
  expect(container.querySelector('[data-chat-messages]')?.className).toContain('overflow-y-auto');
});

test('renders assistant markdown after the stream finishes, including tables', async () => {
  let finish;
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({
      type: 'token',
      text: '**Warmest**\n\n| City | Temp |\n| --- | --- |\n| Nashville | 72 |\n',
    });
    await new Promise((resolve) => {
      finish = resolve;
    });
    onEvent({ type: 'done' });
  });

  const user = userEvent.setup();
  render(<ChatPanel />);

  await user.type(screen.getByLabelText(/message/i), 'compare nashville and atlanta');
  await user.click(screen.getByRole('button', { name: /^send$/i }));

  await waitFor(() => {
    expect(screen.getByText(/\| City \| Temp \|/)).toBeDefined();
  });
  expect(screen.queryByRole('table')).toBeNull();

  finish();

  await waitFor(() => {
    expect(screen.getByRole('table')).toBeDefined();
  });
  expect(screen.getByText('Nashville')).toBeDefined();
  expect(screen.getByText('Warmest').tagName).toBe('STRONG');
  expect(screen.queryByText(/\| City \| Temp \|/)).toBeNull();
});
