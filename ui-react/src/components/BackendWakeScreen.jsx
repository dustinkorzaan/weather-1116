import { logoPinUrl } from '../map/logoPinOverlay';
import { useTheme } from '../theme/useTheme';

/** Blank full-screen loader shown while the backends cold-start (see useBackendWake). */
function BackendWakeScreen() {
  const { resolved } = useTheme();

  return (
    <div
      data-testid="backend-wake-screen"
      role="status"
      aria-live="polite"
      className="flex h-screen w-full flex-col items-center justify-center gap-4 bg-background text-foreground"
    >
      <span className="weather-wake-logo" aria-hidden="true">
        <span className="weather-wake-logo-pulse" />
        <span className="weather-wake-logo-spin">
          <img src={logoPinUrl(resolved)} alt="" className="weather-wake-logo-image" draggable="false" />
        </span>
      </span>
      <p className="text-sm text-muted-foreground">Waking up the weather services…</p>
    </div>
  );
}

export default BackendWakeScreen;
