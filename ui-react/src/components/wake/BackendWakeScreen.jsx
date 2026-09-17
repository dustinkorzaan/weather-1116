/** Blank full-screen loader shown while the backends cold-start; `children` are the per-service WakeTarget rows. */
function BackendWakeScreen({ children }) {
  return (
    <div
      data-testid="backend-wake-screen"
      role="status"
      aria-live="polite"
      className="flex h-screen w-full flex-col items-center justify-center gap-6 bg-background text-foreground"
    >
      <p className="text-sm text-muted-foreground">Waking up the weather services…</p>
      <div className="flex flex-wrap items-center justify-center gap-8 px-6">{children}</div>
      <p className="text-xs text-muted-foreground">Please wait, this can take 30 - 60 seconds…</p>
    </div>
  );
}

export default BackendWakeScreen;
