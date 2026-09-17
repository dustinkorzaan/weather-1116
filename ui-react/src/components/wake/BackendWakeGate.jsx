import { useCallback, useEffect, useState } from 'react';
import BackendWakeScreen from './BackendWakeScreen';
import WakeTarget from './WakeTarget';
import { WAKE_TARGETS } from './wakeTargets';

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
    setWarmState((prev) => (prev[key] ? prev : { ...prev, [key]: true }));
  }, []);

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

  if (!pastGracePeriod) {
    return <div className="h-screen w-full bg-background" />;
  }

  return (
    <BackendWakeScreen>
      {WAKE_TARGETS.map(({ key, label, url }) => (
        <WakeTarget key={key} label={label} url={url} onReady={() => markWarm(key)} />
      ))}
    </BackendWakeScreen>
  );
}
