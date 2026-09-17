import BackendWakeScreen from '../components/BackendWakeScreen';
import { useBackendWake } from './useBackendWake';

// ACA scales every layer to zero when idle; wait for all six (api, mvc,
// blazor, worker, and both MCP hosts) to answer directly (retrying as long
// as it takes) before mounting `children` so cold start happens up front
// instead of mid-navigation.
/** Renders BackendWakeScreen until every backend layer has answered, then mounts `children`. */
export function BackendWakeGate({ children }) {
  const { isWarm, ...statuses } = useBackendWake();

  if (!isWarm) {
    return <BackendWakeScreen statuses={statuses} />;
  }

  return children;
}
