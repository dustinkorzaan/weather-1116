import { useEffect, useState } from 'react';
import {
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../../config/siteLinks';
import { resolveApiBaseUrl } from '../../services/apiBaseUrl';
import BackendWakeScreen from './BackendWakeScreen';
import WakeTarget from './WakeTarget';
import { useWakeTargets } from './useWakeTargets';

// Every layer here scales to zero when idle. API, worker, mcp-srv-app-service, and
// mcp-srv-func-app each expose an anonymous /Wake probe instead of /About -- /About
// does real work a liveness ping doesn't need (API's fans out to the other three,
// worker's and the MCP hosts' introspect their own health). MVC and Blazor have no
// API surface to probe, so a plain GET wakes them.
const WAKE_TARGETS = [
  { key: 'api', label: 'API', url: `${resolveApiBaseUrl()}/Wake` },
  { key: 'worker', label: 'Worker', url: `${workerBaseUrl}/Wake` },
  { key: 'mcpSrvAppService', label: 'MCP App Service', url: `${mcpSrvAppServiceBaseUrl}/Wake` },
  { key: 'mcpSrvFuncApp', label: 'MCP Func App', url: `${mcpSrvFuncAppBaseUrl}/Wake` },
  { key: 'mvc', label: 'MVC', url: mvcBaseUrl },
  { key: 'blazor', label: 'Blazor', url: blazorBaseUrl },
];

// Backends that are already warm still take a real round trip to answer the wake
// pings, so gate the wake screen behind a short grace period -- a fast warm
// response never flashes it, but a genuine cold start still shows it.
const WAKE_SCREEN_GRACE_PERIOD_MS = 250;

/** Renders a WakeTarget per backend layer until every one has answered, then mounts `children`. */
export function BackendWakeGate({ children }) {
  // Pinging is owned here, not by the individual WakeTarget rows -- it starts as
  // soon as BackendWakeGate mounts (immediately, not gated behind the grace
  // period) and survives for as long as the gate itself is mounted, regardless of
  // how its descendants are rendered or re-rendered underneath it.
  const awakeState = useWakeTargets(WAKE_TARGETS);
  const isWarm = Object.values(awakeState).every(Boolean);
  const [pastGracePeriod, setPastGracePeriod] = useState(false);

  useEffect(() => {
    if (isWarm) {
      return undefined;
    }
    const timer = setTimeout(() => setPastGracePeriod(true), WAKE_SCREEN_GRACE_PERIOD_MS);
    return () => clearTimeout(timer);
  }, [isWarm]);

  if (isWarm) {
    return children;
  }

  const wakeTargets = WAKE_TARGETS.map(({ key, label }) => (
    <WakeTarget key={key} label={label} isAwake={awakeState[key]} />
  ));

  return <BackendWakeScreen visible={pastGracePeriod}>{wakeTargets}</BackendWakeScreen>;
}
