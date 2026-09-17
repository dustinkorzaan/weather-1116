import { useEffect, useRef, useState } from 'react';
import { logoPinUrl } from '../../map/logoPinOverlay';
import { useTheme } from '../../theme/useTheme';

// So instead of giving up, fire a fresh ping at this cadence until one lands; earlier
// pings are left running rather than cancelled, since a slow-but-eventually-successful
// request should still count as a win whenever it finishes. This only paces *new* backup
// attempts -- success is detected the instant any one request resolves, not on the next
// tick -- so it's tuned for keeping concurrent in-flight requests low against a
// still-booting (0.25 vCPU/0.5Gi) container rather than for how fast we notice a win.
const WAKE_RETRY_INTERVAL_MS = 30000;

function pingForWakeUp(url) {
  // no-cors: we only care that some request round-tripped, not the response
  // body/status -- mvc/blazor don't send CORS headers to the React origin,
  // and a non-2xx from a /Wake host still means the container is up and serving.
  return fetch(url, { mode: 'no-cors' });
}

/**
 * Pings `url` immediately, then again every ~30s (without waiting for or cancelling
 * earlier attempts) until the first one succeeds, then calls `onReady`. Renders its
 * own logo/label so each backend layer's wake polling is self-contained instead of
 * living in one shared hook.
 */
function WakeTarget({ label, url, onReady }) {
  const [isAwake, setIsAwake] = useState(false);
  const onReadyRef = useRef(onReady);
  onReadyRef.current = onReady;
  const { resolved } = useTheme();
  const logoUrl = logoPinUrl(resolved);

  useEffect(() => {
    let cancelled = false;
    let warm = false;

    const attempt = () => {
      if (warm || cancelled) {
        return;
      }
      pingForWakeUp(url).then(
        () => {
          if (!warm && !cancelled) {
            warm = true;
            setIsAwake(true);
            onReadyRef.current();
          }
        },
        () => {
          // This attempt failed; a still-pending earlier attempt or the next
          // scheduled retry may yet succeed.
        }
      );
    };

    attempt();
    const intervalId = setInterval(() => {
      if (warm || cancelled) {
        clearInterval(intervalId);
        return;
      }
      attempt();
    }, WAKE_RETRY_INTERVAL_MS);

    return () => {
      cancelled = true;
      clearInterval(intervalId);
    };
  }, [url]);

  return (
    <div className="flex flex-col items-center gap-2">
      <span className="weather-wake-logo" aria-hidden="true">
        {!isAwake && <span className="weather-wake-logo-pulse" />}
        <span className={isAwake ? undefined : 'weather-wake-logo-spin'}>
          <img src={logoUrl} alt="" className="weather-wake-logo-image" draggable="false" />
        </span>
      </span>
      <span className="text-xs font-medium text-muted-foreground">
        {label} {isAwake ? 'ready' : 'waking…'}
      </span>
    </div>
  );
}

export default WakeTarget;
