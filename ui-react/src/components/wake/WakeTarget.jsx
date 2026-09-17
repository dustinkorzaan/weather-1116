import { logoPinUrl } from '../../map/logoPinOverlay';
import { useTheme } from '../../theme/useTheme';

/** One backend layer's wake row: its logo/label, reflecting `isAwake` from the caller's wake-state. */
function WakeTarget({ label, isAwake }) {
  const { resolved } = useTheme();

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
