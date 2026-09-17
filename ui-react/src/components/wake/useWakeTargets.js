import { useEffect, useState } from 'react';

const RETRY_DELAY_MS = 30000;

function ping(url) {
  // no-cors: we only care that a request round-tripped, not its body or status --
  // mvc/blazor don't send CORS headers to the React origin, and a non-2xx from a
  // /Wake host still means the container is up and serving.
  return fetch(url, { mode: 'no-cors' });
}

/**
 * Pings every target's url every ~30s until each succeeds, independently.
 * Owned by the caller for its whole lifetime, so ping progress survives
 * whatever the caller's descendants do with their own render tree -- there's
 * no per-row local state that a remount could reset.
 *
 * Returns { [key]: isAwake }.
 */
export function useWakeTargets(targets) {
  const [awakeState, setAwakeState] = useState(() =>
    Object.fromEntries(targets.map(({ key }) => [key, false]))
  );

  useEffect(() => {
    const doneKeys = new Set();

    const tryPing = ({ key, url }) => {
      if (doneKeys.has(key)) {
        return;
      }
      ping(url).then(
        () => {
          doneKeys.add(key);
          setAwakeState((prev) => (prev[key] ? prev : { ...prev, [key]: true }));
        },
        () => {}
      );
    };

    targets.forEach(tryPing);
    const intervalId = setInterval(() => targets.forEach(tryPing), RETRY_DELAY_MS);
    return () => clearInterval(intervalId);
  }, [targets]);

  return awakeState;
}
