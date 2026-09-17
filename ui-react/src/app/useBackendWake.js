import { useEffect, useState } from 'react';
import {
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../config/siteLinks';
import { resolveApiBaseUrl } from '../services/apiBaseUrl';

// ACA scales every layer to zero when idle (see docs/aca-bootstrap.md), and a
// cold start can take up to ~90s per layer. API's own /About handler fans
// out server-side to worker + both MCP hosts (Task.WhenAll), but that fan-out
// only *starts* once the API container has finished its own cold start --
// so waiting on /About alone serializes API's boot in front of theirs. Ping
// worker and both MCP hosts directly, in parallel with API/MVC/Blazor, so
// their cold start begins immediately instead of only after API wakes up
// and gets around to calling them.
// So instead of giving up, fire a fresh ping at this cadence until one
// lands; earlier pings are left running rather than cancelled, since a
// slow-but-eventually-successful request should still count as a win
// whenever it finishes. This only paces *new* backup attempts -- success is
// detected the instant any one request resolves, not on the next tick --
// so it's tuned for keeping concurrent in-flight requests low against a
// still-booting (0.25 vCPU/0.5Gi) container rather than for how fast we
// notice a win.
const WAKE_RETRY_INTERVAL_MS = 30000;

const WAKE_URLS = {
  api: `${resolveApiBaseUrl()}/About`,
  mvc: mvcBaseUrl,
  blazor: blazorBaseUrl,
  // Worker and MCP App Service are ASP.NET [Route("[controller]")] hosts, so
  // /About matches the rest of the repo (AboutController, siteLinks, docs).
  // MCP Func App's HTTP trigger route is literally lowercase "about".
  worker: `${workerBaseUrl}/About`,
  mcpSrvAppService: `${mcpSrvAppServiceBaseUrl}/About`,
  mcpSrvFuncApp: `${mcpSrvFuncAppBaseUrl}/about`,
};

function pingForWakeUp(url) {
  // no-cors: we only care that some request round-tripped, not the response
  // body/status -- mvc/blazor don't send CORS headers to the React origin,
  // and a non-2xx from api still means the container is up and serving.
  return fetch(url, { mode: 'no-cors' });
}

/**
 * Pings `url` immediately, then again every `WAKE_RETRY_INTERVAL_MS` (without
 * waiting for or cancelling earlier attempts) until the first one succeeds.
 * @param {string} url
 * @param {() => boolean} isCancelled
 * @param {() => void} onWarm
 * @returns {() => void} stops scheduling further attempts
 */
function retryUntilAwake(url, isCancelled, onWarm) {
  let isWarm = false;

  const attempt = () => {
    if (isWarm || isCancelled()) {
      return;
    }
    pingForWakeUp(url).then(() => {
      if (!isWarm && !isCancelled()) {
        isWarm = true;
        onWarm();
      }
    }, () => {
      // This attempt failed; a still-pending earlier attempt or the next
      // scheduled retry may yet succeed.
    });
  };

  attempt();
  const intervalId = setInterval(() => {
    if (isWarm || isCancelled()) {
      clearInterval(intervalId);
      return;
    }
    attempt();
  }, WAKE_RETRY_INTERVAL_MS);

  return () => clearInterval(intervalId);
}

const INITIAL_WARM_STATE = {
  api: false,
  mvc: false,
  blazor: false,
  worker: false,
  mcpSrvAppService: false,
  mcpSrvFuncApp: false,
};

/**
 * Waits for the API (via the About endpoint), MVC, Blazor, worker, and both
 * MCP hosts to answer, retrying each independently every ~30s until it does.
 * Returns a warm flag per target plus an overall `isWarm` once every target
 * has answered, so the caller can show per-layer progress instead of one
 * opaque loading state.
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
    isWarm: Object.values(warmState).every(Boolean),
  };
}
