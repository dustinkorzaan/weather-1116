import { logoPinUrl } from '../map/logoPinOverlay';
import { useTheme } from '../theme/useTheme';

const WAKE_TARGETS = [
  { key: 'api', label: 'API' },
  { key: 'mvc', label: 'MVC' },
  { key: 'blazor', label: 'Blazor' },
  { key: 'worker', label: 'Worker' },
  { key: 'mcpSrvAppService', label: 'MCP App Service' },
  { key: 'mcpSrvFuncApp', label: 'MCP Func App' },
];

/** Blank full-screen loader shown while the backends cold-start (see useBackendWake). */
function BackendWakeScreen({ statuses = {} }) {
  const { resolved } = useTheme();
  const logoUrl = logoPinUrl(resolved);

  return (
    <div
      data-testid="backend-wake-screen"
      role="status"
      aria-live="polite"
      className="flex h-screen w-full flex-col items-center justify-center gap-6 bg-background text-foreground"
    >
      <div className="flex flex-wrap items-center justify-center gap-8 px-6">
        {WAKE_TARGETS.map(({ key, label }) => {
          const isAwake = Boolean(statuses[key]);
          return (
            <div key={key} className="flex flex-col items-center gap-2">
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
        })}
      </div>
      <p className="text-sm text-muted-foreground">Waking up the weather services…</p>
      <p className="text-xs text-muted-foreground">Please wait, this can take 30 - 60 seconds...</p>
    </div>
  );
}

export default BackendWakeScreen;
