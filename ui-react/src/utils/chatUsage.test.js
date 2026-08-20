import { expect, test } from 'vitest';
import { formatChatRuntime, formatChatUsageChip, formatChatUsageDetails } from './chatUsage';

test('formats compact chat runtimes', () => {
  expect(formatChatRuntime(0)).toBe('0ms');
  expect(formatChatRuntime(842)).toBe('842ms');
  expect(formatChatRuntime(1000)).toBe('1s');
  expect(formatChatRuntime(1240)).toBe('1.24s');
  expect(formatChatRuntime(10000)).toBe('10s');
  expect(formatChatRuntime(Number.NaN)).toBe('');
});

test('formats a compact usage chip from runtime and total tokens', () => {
  expect(formatChatUsageChip({ runtimeMs: 1240, totalTokenCount: 4218 })).toBe('1.24s · 4,218 tok');
  expect(formatChatUsageChip({ runtimeMs: 842 })).toBe('842ms');
  expect(formatChatUsageChip({ totalTokenCount: 15 })).toBe('15 tok');
  expect(formatChatUsageChip(null)).toBe('');
  expect(formatChatUsageChip(undefined)).toBe('');
});

test('formats usage hover details, omitting missing token fields', () => {
  expect(formatChatUsageDetails({
    runtimeMs: 1240,
    inputTokenCount: 3100,
    cachedTokenCount: 200,
    outputTokenCount: 1118,
    reasoningTokenCount: 40,
    totalTokenCount: 4218,
  })).toBe(
    [
      'Runtime: 1,240 ms',
      'Input: 3,100',
      'Cached: 200',
      'Output: 1,118',
      'Reasoning: 40',
      'Total: 4,218',
    ].join('\n'),
  );

  expect(formatChatUsageDetails({ runtimeMs: 42 })).toBe('Runtime: 42 ms');
  expect(formatChatUsageDetails(null)).toBe('');
});
