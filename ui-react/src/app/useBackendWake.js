import { useEffect, useState } from 'react';
import { blazorBaseUrl, mvcBaseUrl } from '../config/siteLinks';
import { resolveApiBaseUrl } from '../services/apiBaseUrl';

// ACA scales api/mvc/blazor to zero when idle (see docs/aca-bootstrap.md), and
// a cold start can take up to ~90s per layer -- stacked across 2-3 layers
// that can run well past a single fixed timeout. So instead of giving up,
// retry at this cadence until one ping lands. An in-flight ping is never
// cancelled (a slow /About fan-out to worker + MCP hosts still counts as a
// win when it finishes) and is never overlapped -- a merely slow first
// attempt does not start a second mesh fan-out at the 30s tick. Success is
// detected the instant that request resolves, not on the next interval tick.
const WAKE_RETRY_INTERVAL_MS = 30000;

const WAKE_URLS = {
  api: `${resolveApiBaseUrl()}/About`,
  mvc: mvcBaseUrl,
  blazor: blazorBaseUrl,
};

function pingForWakeUp(url) {
  // no-cors: we only care that some request round-tripped, not the response
  // body/status -- mvc/blazor don't send CORS headers to the React origin,
  // and a non-2xx from api still means the container is up and serving.
  return fetch(url, { mode: 'no-cors' });
}

/**
 * Pings `url` immediately, then again every `WAKE_RETRY_INTERVAL_MS` until
 * the first one succeeds. Does not start a new ping while one is already in
 * flight, and does not abort the in-flight one. Stops the interval as soon
 * as a ping succeeds (not on the following tick).
 * @param {string} url
 * @param {() => boolean} isCancelled
 * @param {() => void} onWarm
 * @returns {() => void} stops scheduling further attempts
 */
function retryUntilAwake(url, isCancelled, onWarm) {
  let isWarm = false;
  let inFlight = false;
  let intervalId;

  const stop = () => {
    if (intervalId !== undefined) {
      clearInterval(intervalId);
      intervalId = undefined;
    }
  };

  const attempt = () => {
    if (isWarm || isCancelled() || inFlight) {
      return;
    }
    inFlight = true;
    pingForWakeUp(url).then(
      () => {
        inFlight = false;
        if (!isWarm && !isCancelled()) {
          isWarm = true;
          stop();
          onWarm();
        }
      },
      () => {
        inFlight = false;
        // This attempt failed; the next interval tick may retry.
      }
    );
  };

  attempt();
  intervalId = setInterval(() => {
    if (isWarm || isCancelled()) {
      stop();
      return;
    }
    attempt();
  }, WAKE_RETRY_INTERVAL_MS);

  return stop;
}

const INITIAL_WARM_STATE = { api: false, mvc: false, blazor: false };

/**
 * Waits for the API (via the About endpoint), MVC, and Blazor apps to answer,
 * retrying each independently every ~30s on failure (not while a ping is
 * still in flight) until it does. Returns a warm flag
 * per target plus an overall `isWarm` once all three have answered, so the
 * caller can show per-layer progress instead of one opaque loading state.
 */
export function useBackendWake() {
  const [warmState, setWarmState] = useState(INITIAL_WARM_STATE);

  useEffect(() => {
    let cancelled = false;
    const isCancelled = () => cancelled;
    const markWarm = (key) => setWarmState((prev) => (prev[key] ? prev : { ...prev, [key]: true }));

    const stopCallbacks = Object.entries(WAKE_URLS).map(([key, url]) =>
      retryUntilAwake(url, isCancelled, () => markWarm(key))
    );

    return () => {
      cancelled = true;
      stopCallbacks.forEach((stop) => stop());
    };
  }, []);

  return {
    ...warmState,
    isWarm: warmState.api && warmState.mvc && warmState.blazor,
  };
}
