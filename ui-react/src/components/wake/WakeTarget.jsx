import { useEffect, useState } from 'react';
import { logoPinUrl } from '../../map/logoPinOverlay';
import { useTheme } from '../../theme/useTheme';

const RETRY_DELAY_MS = 30000;

function ping(url) {
  // no-cors: we only care that a request round-tripped, not its body or status --
  // mvc/blazor don't send CORS headers to the React origin, and a non-2xx from a
  // /Wake host still means the container is up and serving.
  return fetch(url, { mode: 'no-cors' });
}

/** Pings `url` every ~30s until one attempt succeeds; earlier attempts are left running rather than cancelled, so a slow one can still win. */
function useWake(url) {
  const [isAwake, setIsAwake] = useState(false);

  useEffect(() => {
    let done = false;

    const tryPing = () => {
      if (done) {
        return;
      }
      ping(url).then(
        () => {
          done = true;
          setIsAwake(true);
        },
        () => {}
      );
    };

    tryPing();
    const intervalId = setInterval(tryPing, RETRY_DELAY_MS);
    return () => clearInterval(intervalId);
  }, [url]);

  return isAwake;
}

/** One backend layer's wake row: its logo/label, plus reporting `wakeKey` ready once `url` answers. */
function WakeTarget({ wakeKey, label, url, onReady }) {
  const isAwake = useWake(url);
  const { resolved } = useTheme();

  useEffect(() => {
    if (isAwake) {
      onReady(wakeKey);
    }
  }, [isAwake, onReady, wakeKey]);

  return (
    <div className="flex flex-col items-center gap-2">
      <span className="weather-wake-logo" aria-hidden="true">
        {!isAwake && <span className="weather-wake-logo-pulse" />}
        <span className={isAwake ? undefined : 'weather-wake-logo-spin'}>
          <img src={logoPinUrl(resolved)} alt="" className="weather-wake-logo-image" draggable="false" />
        </span>
      </span>
      <span className="text-xs font-medium text-muted-foreground">
        {label} {isAwake ? 'ready' : 'waking…'}
      </span>
    </div>
  );
}

export default WakeTarget;
