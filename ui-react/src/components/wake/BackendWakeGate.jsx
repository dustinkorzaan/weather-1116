import { useCallback, useEffect, useState } from 'react';
import {
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mcpSrvNodeBaseUrl,
  mcpSrvPythonBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../../config/siteLinks';
import { resolveApiBaseUrl } from '../../services/apiBaseUrl';
import BackendWakeScreen from './BackendWakeScreen';
import WakeTarget from './WakeTarget';

// Every layer here scales to zero when idle. API, worker, mcp-srv-app-service,
// mcp-srv-func-app, mcp-srv-python, and mcp-srv-node each expose an anonymous /Wake probe
// instead of /About -- /About does real work a liveness ping doesn't need
// (API's fans out to the other three, worker's and the MCP hosts' introspect
// their own health). MVC and Blazor have no API surface to probe, so a plain
// GET wakes them.
const WAKE_TARGETS = [
  { key: 'api', label: 'API', url: `${resolveApiBaseUrl()}/Wake` },
  { key: 'worker', label: 'Worker', url: `${workerBaseUrl}/Wake` },
  { key: 'mcpSrvAppService', label: 'MCP App Service', url: `${mcpSrvAppServiceBaseUrl}/Wake` },
  { key: 'mcpSrvFuncApp', label: 'MCP Func App', url: `${mcpSrvFuncAppBaseUrl}/Wake` },
  { key: 'mcpSrvPython', label: 'MCP Python', url: `${mcpSrvPythonBaseUrl}/Wake` },
  { key: 'mcpSrvNode', label: 'MCP Node', url: `${mcpSrvNodeBaseUrl}/Wake` },
  { key: 'mvc', label: 'MVC', url: mvcBaseUrl },
  { key: 'blazor', label: 'Blazor', url: blazorBaseUrl },
];

const INITIAL_WARM_STATE = Object.fromEntries(WAKE_TARGETS.map(({ key }) => [key, false]));

// Backends that are already warm still take a real round trip to answer the wake
// pings, so gate the wake screen behind a short grace period -- a fast warm
// response never flashes it, but a genuine cold start still shows it.
const WAKE_SCREEN_GRACE_PERIOD_MS = 250;

/** Renders a WakeTarget per backend layer until every one has answered, then mounts `children`. */
export function BackendWakeGate({ children }) {
  const [warmState, setWarmState] = useState(INITIAL_WARM_STATE);
  const isWarm = Object.values(warmState).every(Boolean);
  const [pastGracePeriod, setPastGracePeriod] = useState(false);

  const markWarm = useCallback((key) => {
    setWarmState((prev) => ({ ...prev, [key]: true }));
  }, []);

  useEffect(() => {
    if (isWarm) {
      return undefined;
    }
    const timer = setTimeout(() => setPastGracePeriod(true), WAKE_SCREEN_GRACE_PERIOD_MS);
    return () => clearTimeout(timer);
  }, [isWarm]);

  // Mounted unconditionally (even during the grace period) so the pings fire
  // immediately on load -- if they only started once the visible wake screen
  // mounted, the grace period could never be long enough to absorb a fast
  // warm response, and the wake screen would always flash for at least one
  // round trip.
  const wakeTargets = WAKE_TARGETS.map(({ key, label, url }) => (
    <WakeTarget key={key} wakeKey={key} label={label} url={url} onReady={markWarm} />
  ));

  if (isWarm) {
    return children;
  }

  // Always the same element type (BackendWakeScreen) in the same tree position --
  // only its `visible` prop changes across the grace period. Swapping between two
  // different element types here (e.g. a plain div pre-grace-period vs
  // BackendWakeScreen after) would make React tear down and remount every
  // WakeTarget on that transition, resetting any that had already answered back
  // to "waking…" and losing their progress.
  return <BackendWakeScreen visible={pastGracePeriod}>{wakeTargets}</BackendWakeScreen>;
}
