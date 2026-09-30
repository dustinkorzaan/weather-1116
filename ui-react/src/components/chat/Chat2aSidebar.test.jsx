import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Chat2aSidebar from './Chat2aSidebar';
import { streamChatMessage } from '../../utils/chatStream';
import { useMapPins } from '../../map/mapPinsContext';

vi.mock('../../utils/chatStream', () => ({
  streamChatMessage: vi.fn(),
}));

vi.mock('../../map/mapPinsContext', () => ({
  useMapPins: vi.fn(),
}));

afterEach(() => {
  vi.resetAllMocks();
});

function renderSidebar({ open = true, onClose = vi.fn() } = {}) {
  const refreshCities = vi.fn().mockResolvedValue(undefined);
  useMapPins.mockReturnValue({ refreshCities });
  const utils = render(<Chat2aSidebar open={open} onClose={onClose} />);
  return { ...utils, refreshCities, onClose };
}

async function send(user, text) {
  await user.type(screen.getByLabelText(/message/i), text);
  await user.click(screen.getByRole('button', { name: /^send$/i }));
}

test('is labelled Chat2a and hidden while closed', () => {
  const { container } = renderSidebar({ open: false });

  const panel = container.querySelector('#chat2a-sidebar');
  expect(panel.getAttribute('role')).toBe('complementary');
  expect(panel.getAttribute('aria-label')).toBe('Chat2a');
  expect(panel.hidden).toBe(true);
  expect(screen.queryByRole('complementary', { name: 'Chat2a' })).toBeNull();
});

test('Close chat button and Escape call onClose', async () => {
  const user = userEvent.setup();
  const { onClose } = renderSidebar();

  await user.click(screen.getByRole('button', { name: 'Close chat' }));
  await user.keyboard('{Escape}');

  expect(onClose).toHaveBeenCalledTimes(2);
});

test('sends to Chat2a without gates and reuses the streamed session id', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'session', sessionId: 'session-1' });
    onEvent({ type: 'token', text: 'Added.' });
    onEvent({ type: 'done' });
  });
  const user = userEvent.setup();
  renderSidebar();

  await send(user, 'add Nashville');
  await waitFor(() => expect(screen.getByText('Added.')).toBeDefined());
  await send(user, 'remove Nashville');

  await waitFor(() => expect(streamChatMessage).toHaveBeenCalledTimes(2));
  const [first, second] = streamChatMessage.mock.calls.map(([args]) => args);
  expect(first).toMatchObject({ endpoint: '/Chat2a/messages', sessionId: null, message: 'add Nashville' });
  expect(first.gates).toBeUndefined();
  expect(second).toMatchObject({ endpoint: '/Chat2a/messages', sessionId: 'session-1', message: 'remove Nashville' });
});

test('renders tool lines and error events', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'tool_start', toolName: 'AddUserCity', toolArguments: '{}' });
    onEvent({ type: 'tool_end', toolName: 'AddUserCity', toolResult: 'ok' });
    onEvent({ type: 'error', errorMessage: 'Agent failed.' });
  });
  const user = userEvent.setup();
  renderSidebar();

  await send(user, 'add Nashville');

  await waitFor(() => expect(screen.getByText('Ran AddUserCity …')).toBeDefined());
  expect(screen.getByText('Agent failed.')).toBeDefined();
});

test('refreshes cities once per completed send, including failed sends', async () => {
  streamChatMessage
    .mockImplementationOnce(async ({ onEvent }) => {
      onEvent({ type: 'token', text: 'Done.' });
      onEvent({ type: 'done' });
    })
    .mockRejectedValueOnce(new Error('Chat request failed (500)'));
  const user = userEvent.setup();
  const { refreshCities } = renderSidebar();

  await send(user, 'first');
  await waitFor(() => expect(refreshCities).toHaveBeenCalledTimes(1));

  await send(user, 'second');
  await waitFor(() => expect(screen.getByText('Chat request failed (500)')).toBeDefined());
  await waitFor(() => expect(refreshCities).toHaveBeenCalledTimes(2));
});
