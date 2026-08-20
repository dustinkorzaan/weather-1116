import { formatRunLogMs, formatRunLogTokenCount } from './aiWeatherDisplay';

function trimTrailingZeros(value) {
  return value.replace(/\.?0+$/, '');
}

/** Formats a compact duration, e.g. "842ms" or "1.24s". */
export function formatChatRuntime(ms) {
  if (!Number.isFinite(ms)) {
    return '';
  }

  const rounded = Math.round(ms);
  if (rounded < 1000) {
    return `${rounded}ms`;
  }

  return `${trimTrailingZeros((rounded / 1000).toFixed(2))}s`;
}

/** Formats the visible assistant-row chip, e.g. "1.24s · 4,218 tok". */
export function formatChatUsageChip(usage) {
  if (!usage) {
    return '';
  }

  const parts = [];
  const runtime = formatChatRuntime(usage.runtimeMs);
  if (runtime) {
    parts.push(runtime);
  }

  const tokens = formatRunLogTokenCount(usage.totalTokenCount);
  if (tokens) {
    parts.push(`${tokens} tok`);
  }

  return parts.join(' · ');
}

/** Formats the hover breakdown for a usage chip. */
export function formatChatUsageDetails(usage) {
  if (!usage) {
    return '';
  }

  const lines = [];
  if (Number.isFinite(usage.runtimeMs)) {
    lines.push(`Runtime: ${formatRunLogMs(usage.runtimeMs)} ms`);
  }

  addTokenLine(lines, 'Input', usage.inputTokenCount);
  addTokenLine(lines, 'Cached', usage.cachedTokenCount);
  addTokenLine(lines, 'Output', usage.outputTokenCount);
  addTokenLine(lines, 'Reasoning', usage.reasoningTokenCount);
  addTokenLine(lines, 'Total', usage.totalTokenCount);
  return lines.join('\n');
}

function addTokenLine(lines, label, tokens) {
  const formatted = formatRunLogTokenCount(tokens);
  if (formatted) {
    lines.push(`${label}: ${formatted}`);
  }
}
