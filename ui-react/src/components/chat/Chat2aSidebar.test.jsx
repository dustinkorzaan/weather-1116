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
  expect(container.querySelector('[data-chat2a-sidebar-messages] [data-tool-details]')).toBeNull();
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
