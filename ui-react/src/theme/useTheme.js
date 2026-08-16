import { useEffect, useState } from 'react';
import {
  THEME_CHANGE_EVENT,
  applyTheme,
  getPreference,
  resolveTheme,
  subscribeToSystemTheme,
} from './theme';

export function useTheme() {
  const [preference, setPreferenceState] = useState(() => getPreference());
  const [resolved, setResolved] = useState(() => resolveTheme());

  useEffect(() => {
    const applied = applyTheme(getPreference());
    setPreferenceState(applied.preference);
    setResolved(applied.resolved);

    const onTheme = (event) => {
      setPreferenceState(event.detail.preference);
      setResolved(event.detail.resolved);
    };
    window.addEventListener(THEME_CHANGE_EVENT, onTheme);

    const unsubscribe = subscribeToSystemTheme(() => {
      if (getPreference() === 'system') {
        const next = applyTheme('system');
        setPreferenceState(next.preference);
        setResolved(next.resolved);
      }
    });

    return () => {
      window.removeEventListener(THEME_CHANGE_EVENT, onTheme);
      unsubscribe();
    };
  }, []);

  return {
    preference,
    resolved,
    setPreference: (next) => {
      const applied = applyTheme(next);
      setPreferenceState(applied.preference);
      setResolved(applied.resolved);
    },
  };
}
