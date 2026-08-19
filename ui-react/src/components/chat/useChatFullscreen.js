import { useCallback, useEffect, useState } from 'react';

export function nativeFullscreenElement() {
  return document.fullscreenElement || document.webkitFullscreenElement || null;
}

export function useChatFullscreen(ref) {
  const [mode, setMode] = useState('off');
  const isFullscreen = mode !== 'off';

  useEffect(() => {
    function onChange() {
      const el = ref.current;
      const active = !!el && nativeFullscreenElement() === el;
      setMode((current) => {
        if (active) {
          return 'native';
        }
        if (current === 'native') {
          return 'off';
        }
        return current;
      });
    }

    document.addEventListener('fullscreenchange', onChange);
    document.addEventListener('webkitfullscreenchange', onChange);
    return () => {
      document.removeEventListener('fullscreenchange', onChange);
      document.removeEventListener('webkitfullscreenchange', onChange);
    };
  }, [ref]);

  useEffect(() => {
    function onKey(event) {
      if (event.key === 'Escape') {
        setMode((current) => (current === 'css' ? 'off' : current));
      }
    }

    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, []);

  const toggle = useCallback(async () => {
    const el = ref.current;
    if (!el) {
      return;
    }

    if (mode !== 'off') {
      const exit = document.exitFullscreen || document.webkitExitFullscreen;
      if (mode === 'native' && typeof exit === 'function') {
        try {
          await exit.call(document);
        } catch {
          // Ignore browsers that reject exit outside a user gesture.
        }
      }
      setMode('off');
      return;
    }

    const req = el.requestFullscreen || el.webkitRequestFullscreen;
    if (typeof req === 'function') {
      try {
        await req.call(el);
        setMode('native');
        return;
      } catch {
        // Fall back to a viewport-filling layout when the Fullscreen API is blocked.
      }
    }

    setMode('css');
  }, [mode, ref]);

  return { isFullscreen, isCssFullscreen: mode === 'css', toggle };
}
