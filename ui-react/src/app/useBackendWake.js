import { useEffect, useState } from 'react';
import { blazorBaseUrl, mvcBaseUrl } from '../config/siteLinks';

// api/mvc/blazor all scale to zero when idle (see docs/app-service-bootstrap.md).
// Give a cold start this long to answer before giving up and showing the app
// anyway -- a broken backend shouldn't strand the user on a blank screen.
const WAKE_TIMEOUT_MS = 45000;

function withTimeout(promise, ms) {
  let timeoutId;
  const timeout = new Promise((resolve) => {
    timeoutId = setTimeout(resolve, ms);
  });

  return Promise.race([promise, timeout]).finally(() => clearTimeout(timeoutId));
}

function pingForWakeUp(url) {
  // no-cors: we only care that the request round-tripped, not the response
  // body/status, and mvc/blazor don't send CORS headers to the React origin.
  return fetch(url, { mode: 'no-cors' }).catch(() => {});
}

/**
 * Waits for the API (via the About endpoint), MVC, and Blazor apps to answer
 * at least once, so the caller can hold the UI behind a wake-up screen
 * instead of letting the user hit a cold-starting backend mid-navigation.
 * @param {() => Promise<unknown>} loadAbout - RTK Query lazy trigger; already
 *   resolves (never rejects) once the API responds or errors.
 */
export function useBackendWake(loadAbout) {
  const [isWarm, setIsWarm] = useState(false);

  useEffect(() => {
    let cancelled = false;

    Promise.all([
      withTimeout(loadAbout(), WAKE_TIMEOUT_MS),
      withTimeout(pingForWakeUp(blazorBaseUrl), WAKE_TIMEOUT_MS),
      withTimeout(pingForWakeUp(mvcBaseUrl), WAKE_TIMEOUT_MS),
    ]).then(() => {
      if (!cancelled) {
        setIsWarm(true);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [loadAbout]);

  return isWarm;
}
