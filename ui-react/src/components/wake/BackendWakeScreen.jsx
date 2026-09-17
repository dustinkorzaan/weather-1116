/**
 * Full-screen loader shown while the backends cold-start; `children` are the per-service
 * WakeTarget rows. Stays mounted with the same element structure whether `visible` is true
 * or false -- only classes/attributes toggle -- so the WakeTarget children (and their
 * in-flight pings) survive the grace-period transition instead of being remounted.
 */
function BackendWakeScreen({ children, visible }) {
  return (
    <div
      data-testid={visible ? 'backend-wake-screen' : undefined}
      role={visible ? 'status' : undefined}
      aria-live={visible ? 'polite' : undefined}
      className={
        visible
          ? 'flex h-screen w-full flex-col items-center justify-center gap-6 bg-background text-foreground'
          : 'hidden'
      }
    >
      <p className="text-sm text-muted-foreground">Waking up the weather services…</p>
      <div className="flex flex-wrap items-center justify-center gap-8 px-6">{children}</div>
      <p className="text-xs text-muted-foreground">Please wait, this can take 30 - 60 seconds…</p>
    </div>
  );
}

export default BackendWakeScreen;
