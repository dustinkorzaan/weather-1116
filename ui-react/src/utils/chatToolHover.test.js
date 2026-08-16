import { afterEach, expect, test, vi } from 'vitest';
import { formatToolHoverText } from './chatToolHover';

afterEach(() => {
  vi.restoreAllMocks();
});

test('formatToolHoverText joins arguments and result', () => {
  expect(
    formatToolHoverText({
      toolArguments: '{\n  "location": "Nashville, TN"\n}',
      toolResult: '[\n  {\n    "name": "Nashville"\n  }\n]',
    })
  ).toBe(
    'Arguments\n{\n  "location": "Nashville, TN"\n}\n\nResult\n[\n  {\n    "name": "Nashville"\n  }\n]'
  );
});

test('formatToolHoverText shows a waiting hint while the tool is running', () => {
  expect(formatToolHoverText({ running: true })).toBe('Waiting for tool output…');
  expect(formatToolHoverText({ running: false })).toBe('');
});
