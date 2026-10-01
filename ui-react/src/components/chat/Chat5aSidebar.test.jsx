import { afterEach, expect, test, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Chat5aSidebar from './Chat5aSidebar';
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
  const utils = render(<Chat5aSidebar open={open} onClose={onClose} />);
  return { ...utils, refreshCities, onClose };
}

async function send(user, text) {
  await user.type(screen.getByLabelText(/message/i), text);
  await user.click(screen.getByRole('button', { name: /^send$/i }));
}

test('does not listen for Escape while closed', async () => {
  const user = userEvent.setup();
  const { onClose } = renderSidebar({ open: false });

  await user.keyboard('{Escape}');

  expect(onClose).not.toHaveBeenCalled();
});

test('shows the streaming reply as plain text, then markdown without a usage chip when done has no usage', async () => {
  let finish;
  streamChatMessage.mockImplementation(({ onEvent }) => new Promise((resolve) => {
    onEvent({ type: 'token', text: '**Added.**' });
    finish = () => {
      onEvent({ type: 'done' });
      resolve();
    };
  }));
  const user = userEvent.setup();
  const { container } = renderSidebar();

  await send(user, 'add Nashville');
  await waitFor(() => expect(screen.getByText('**Added.**')).toBeDefined());
  finish();

  await waitFor(() => expect(container.querySelector('strong')?.textContent).toBe('Added.'));
  expect(container.querySelector('[data-chat5a-sidebar-messages] [data-tool-details]')).toBeNull();
});

test('a failed /User refresh is logged and the composer re-enables', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'token', text: 'Done.' });
    onEvent({ type: 'done' });
  });
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
  const user = userEvent.setup();
  const { refreshCities } = renderSidebar();
  refreshCities.mockRejectedValueOnce(new Error('offline'));

  await send(user, 'add Nashville');

  await waitFor(() => expect(consoleError).toHaveBeenCalledWith('Failed to refresh cities:', expect.any(Error)));
  expect(screen.getByLabelText(/message/i).disabled).toBe(false);
  expect(screen.getByRole('button', { name: /^send$/i }).disabled).toBe(false);
});

test('sends the sidebar gate defaults (Code Input and LLM Output off) and follows checkbox toggles', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'done' });
  });
  const user = userEvent.setup();
  renderSidebar();

  const labels = screen.getAllByRole('checkbox').map((box) => box.closest('label').textContent);
  expect(labels).toEqual(['500 Char', 'Code Input', 'LLM Input', 'Sys Prompt', 'LLM Output (waits for full reply)']);

  await send(user, 'add Nashville');
  await waitFor(() => expect(streamChatMessage).toHaveBeenCalledTimes(1));
  expect(streamChatMessage.mock.calls[0][0]).toMatchObject({
    endpoint: '/Chat5a/messages',
    gates: { maxLength: true, ruleInput: false, llmInput: true, systemPrompt: true, llmOutput: false },
  });

  await waitFor(() => expect(screen.getByLabelText(/message/i).disabled).toBe(false));
  await user.click(screen.getByRole('checkbox', { name: 'Code Input' }));
  await send(user, 'remove Austin');
  await waitFor(() => expect(streamChatMessage).toHaveBeenCalledTimes(2));
  expect(streamChatMessage.mock.calls[1][0].gates.ruleInput).toBe(true);
});

test('renders a blocked event as a blocked entry', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({ type: 'blocked', errorMessage: 'Only weather, location and saved-city requests.' });
    onEvent({ type: 'done' });
  });
  const user = userEvent.setup();
  renderSidebar();

  await send(user, 'Tell me a joke.');

  const entry = await screen.findByText('Only weather, location and saved-city requests.');
  expect(entry.closest('[class*="amber"]')).not.toBeNull();
});

test('returns focus to the textarea after a send completes or fails', async () => {
  streamChatMessage
    .mockImplementationOnce(async ({ onEvent }) => {
      onEvent({ type: 'token', text: 'Saved.' });
      onEvent({ type: 'done' });
    })
    .mockRejectedValueOnce(new Error('offline'));
  const user = userEvent.setup();
  renderSidebar();
  const textarea = screen.getByLabelText(/message/i);

  await send(user, 'add Nashville');
  await waitFor(() => expect(document.activeElement).toBe(textarea));
  expect(textarea.disabled).toBe(false);

  await send(user, 'add Denver');
  await screen.findByText('offline');
  await waitFor(() => expect(document.activeElement).toBe(textarea));
});
