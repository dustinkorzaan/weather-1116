import { useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import {
  apiBaseUrl,
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mcpSrvNodeBaseUrl,
  mcpSrvPythonBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../../config/siteLinks';
import { useLazyGetAboutQuery } from '../../services/weatherApi';
import { AboutTreeNode } from './AboutTreeNode';

const SITE_LINKS = [
  { label: 'UI Blazor', href: blazorBaseUrl },
  { label: 'MVC', href: mvcBaseUrl },
  { label: 'API About', href: `${apiBaseUrl}/About` },
  { label: 'Worker Hangfire', href: `${workerBaseUrl}/hangfire` },
  { label: 'MCP App Service About', href: `${mcpSrvAppServiceBaseUrl}/About` },
  { label: 'MCP Func App About', href: `${mcpSrvFuncAppBaseUrl}/about` },
  { label: 'MCP Python About', href: `${mcpSrvPythonBaseUrl}/About` },
  { label: 'MCP Node About', href: `${mcpSrvNodeBaseUrl}/About` },
];

function SiteLinksFooter() {
  return (
    <div className="mt-4 flex flex-wrap gap-x-4 gap-y-3 border-t border-border pt-3 text-sm">
      {SITE_LINKS.map((link) => (
        <a
          key={link.label}
          className="text-foreground/80 hover:underline"
          href={link.href}
          target="_blank"
          rel="noopener noreferrer"
        >
          {link.label}
        </a>
      ))}
      <a
        className="text-foreground/80 hover:underline"
        href="https://github.com/dustinkorzaan/weather-1116"
        target="_blank"
        rel="noopener noreferrer"
      >
        GitHub
      </a>
    </div>
  );
}

/** The header's "About" dialog: fetches the About tree lazily each time it's opened. */
function AboutDialog({ open, onOpenChange }) {
  const [loadAbout, aboutQuery] = useLazyGetAboutQuery();

  useEffect(() => {
    if (open) {
      loadAbout();
    }
  }, [open, loadAbout]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-3rem)] overflow-y-auto sm:max-h-[calc(100dvh-5rem)] sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>About</DialogTitle>
        </DialogHeader>
        <div className="min-h-16">
          {aboutQuery.isFetching && (
            <p className="inline-flex items-center gap-2 text-muted-foreground">
              <span
                className="size-4 animate-spin rounded-full border-2 border-border border-t-foreground"
                aria-hidden="true"
              />
              <span>Loading About information...</span>
            </p>
          )}
          {!aboutQuery.isFetching && aboutQuery.isError && (
            <p className="text-destructive">Unable to load About information.</p>
          )}
          {!aboutQuery.isFetching && !aboutQuery.isError && aboutQuery.data && (
            <ul className="list-disc pl-5">
              <AboutTreeNode node={aboutQuery.data} />
            </ul>
          )}
          <SiteLinksFooter />
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default AboutDialog;
