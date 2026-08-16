import { afterEach, expect, test, vi } from 'vitest';
import {
  THEME_STORAGE_KEY,
  applyTheme,
  getPreference,
  normalizePreference,
  resolveTheme,
} from './theme';

afterEach(() => {
  document.documentElement.classList.remove('dark');
  document.documentElement.removeAttribute('data-theme');
  document.documentElement.removeAttribute('data-theme-preference');
  window.localStorage.removeItem(THEME_STORAGE_KEY);
  vi.restoreAllMocks();
});

test('normalizePreference falls back to system', () => {
  expect(normalizePreference('light')).toBe('light');
  expect(normalizePreference('dark')).toBe('dark');
  expect(normalizePreference('system')).toBe('system');
  expect(normalizePreference('nope')).toBe('system');
  expect(normalizePreference(null)).toBe('system');
});

test('applyTheme dark adds the html class and persists the preference', () => {
  const result = applyTheme('dark');

  expect(result).toEqual({ preference: 'dark', resolved: 'dark' });
  expect(document.documentElement.classList.contains('dark')).toBe(true);
  expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  expect(document.documentElement.getAttribute('data-theme-preference')).toBe('dark');
  expect(getPreference()).toBe('dark');
});

test('applyTheme light removes the html class', () => {
  applyTheme('dark');
  applyTheme('light');

  expect(document.documentElement.classList.contains('dark')).toBe(false);
  expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  expect(getPreference()).toBe('light');
});

test('resolveTheme system follows matchMedia', () => {
  window.matchMedia = vi.fn((query) => ({
    matches: String(query).includes('dark'),
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  }));

  expect(resolveTheme('system')).toBe('dark');
});
