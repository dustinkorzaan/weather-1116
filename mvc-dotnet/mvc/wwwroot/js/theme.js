window.weatherTheme = (function () {
  const STORAGE_KEY = 'weather-theme';
  const PREFERENCES = ['light', 'dark', 'system'];
  const CHANGE_EVENT = 'weather-theme-change';

  function getSystemTheme() {
    if (typeof window.matchMedia !== 'function') {
      return 'light';
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function normalizePreference(value) {
    return PREFERENCES.indexOf(value) === -1 ? 'system' : value;
  }

  function getPreference() {
    try {
      return normalizePreference(window.localStorage.getItem(STORAGE_KEY));
    } catch {
      return 'system';
    }
  }

  function resolve(preference) {
    const normalized = normalizePreference(preference);
    return normalized === 'system' ? getSystemTheme() : normalized;
  }

  function apply(preference) {
    const normalized = normalizePreference(preference);
    const resolved = resolve(normalized);
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
      window.localStorage.setItem(STORAGE_KEY, normalized);
    } catch {
      // Ignore quota / private-mode failures.
    }

    window.dispatchEvent(
      new CustomEvent(CHANGE_EVENT, {
        detail: { preference: normalized, resolved: resolved },
      })
    );

    return { preference: normalized, resolved: resolved };
  }

  function setPreference(preference) {
    return apply(preference);
  }

  function subscribeToSystemTheme() {
    if (typeof window.matchMedia !== 'function') {
      return;
    }
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    media.addEventListener('change', function () {
      if (getPreference() === 'system') {
        apply('system');
      }
    });
  }

  function syncOptionButtons() {
    const preference = getPreference();
    document.querySelectorAll('[data-theme-option]').forEach(function (button) {
      const value = button.getAttribute('data-theme-option');
      const selected = value === preference;
      button.setAttribute('aria-checked', selected ? 'true' : 'false');
      const label = button.getAttribute('data-theme-label') || button.textContent.replace(/\s*✓\s*$/, '');
      button.setAttribute('data-theme-label', label);
      button.textContent = selected ? label + ' ✓' : label;
    });
  }

  function bindMenu() {
    document.querySelectorAll('[data-theme-option]').forEach(function (button) {
      button.addEventListener('click', function (event) {
        event.preventDefault();
        setPreference(button.getAttribute('data-theme-option'));
        syncOptionButtons();
      });
    });
    window.addEventListener(CHANGE_EVENT, syncOptionButtons);
    syncOptionButtons();
  }

  apply(getPreference());
  subscribeToSystemTheme();

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindMenu);
  } else {
    bindMenu();
  }

  return {
    getPreference: getPreference,
    resolve: resolve,
    setPreference: setPreference,
    apply: apply,
  };
})();
