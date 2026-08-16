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

test('shows tool arguments and result on hover', async () => {
  streamChatMessage.mockImplementation(async ({ onEvent }) => {
    onEvent({
      type: 'tool_start',
      toolName: 'GetLatLongData',
      toolArguments: '{\n  "location": "Nashville, TN"\n}',
    });
    onEvent({
      type: 'tool_end',
      toolName: 'GetLatLongData',
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

  const chip = await screen.findByText('Ran GetLatLongData …');
  await user.hover(chip);

  await waitFor(() => {
    expect(screen.getByRole('tooltip').textContent).toContain('Arguments');
    expect(screen.getByRole('tooltip').textContent).toContain('Nashville, TN');
    expect(screen.getByRole('tooltip').textContent).toContain('Result');
    expect(screen.getByRole('tooltip').textContent).toContain('"name": "Nashville"');
  });
});
