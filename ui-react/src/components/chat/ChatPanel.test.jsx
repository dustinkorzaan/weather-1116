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

test('scrolls to the bottom when switching among the four chats', async () => {
  stubChatMessagesScrollHeight(640);
  const user = userEvent.setup();
  const { container } = render(<ChatPanel />);

  for (const name of ['Chat1b', 'Chat2a', 'Chat2b', 'Chat1a']) {
    await user.click(screen.getByRole('tab', { name }));
    expect(container.querySelector('[data-chat-messages]').scrollTop).toBe(640);
  }
});
