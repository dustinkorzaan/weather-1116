import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';

/** Placeholder for tabs whose data isn't wired up yet (Phase 2/3). */
function ComingSoonTab({ label, icon: Icon }) {
  return (
    <div className="flex min-h-40 flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border p-8 text-center text-muted-foreground">
      <div className="flex items-center gap-2">
        {Icon && <Icon className="size-5" aria-hidden="true" />}
        <span className="font-medium text-foreground">{label}</span>
        <Button type="button" variant="ghost" size="icon-sm" disabled aria-label="Refresh">
          <RefreshCw />
        </Button>
      </div>
      <p className="text-sm">Coming soon.</p>
    </div>
  );
}

export default ComingSoonTab;
