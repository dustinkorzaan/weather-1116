export const THEME_STORAGE_KEY = 'weather-theme';
export const THEME_PREFERENCES = ['light', 'dark', 'system'];
export const THEME_CHANGE_EVENT = 'weather-theme-change';

export function getSystemTheme() {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return 'light';
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function normalizePreference(value) {
  return THEME_PREFERENCES.includes(value) ? value : 'system';
}

export function getPreference() {
  if (typeof window === 'undefined' || !window.localStorage) {
    return 'system';
  }

  try {
    return normalizePreference(window.localStorage.getItem(THEME_STORAGE_KEY));
  } catch {
    return 'system';
  }
}

export function resolveTheme(preference = getPreference()) {
  const normalized = normalizePreference(preference);
  return normalized === 'system' ? getSystemTheme() : normalized;
}

export function applyTheme(preference) {
  const normalized = normalizePreference(preference);
  const resolved = resolveTheme(normalized);

  if (typeof document === 'undefined') {
    return { preference: normalized, resolved };
  }

  const root = document.documentElement;
  root.classList.toggle('dark', resolved === 'dark');
  root.setAttribute('data-theme', resolved);
  root.setAttribute('data-theme-preference', normalized);
  root.style.colorScheme = resolved;

  const meta = document.querySelector('meta[name="theme-color"]');
  if (meta) {
    meta.setAttribute('content', resolved === 'dark' ? '#111827' : '#ffffff');
  }

  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, normalized);
  } catch {
    // Ignore quota / private-mode failures.
  }

  window.dispatchEvent(
    new CustomEvent(THEME_CHANGE_EVENT, {
      detail: { preference: normalized, resolved },
    })
  );

  return { preference: normalized, resolved };
}

export function subscribeToSystemTheme(onChange) {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return () => {};
  }

  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const handler = () => onChange(getSystemTheme());
  media.addEventListener('change', handler);
  return () => media.removeEventListener('change', handler);
}
