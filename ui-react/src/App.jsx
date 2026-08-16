import { useState } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import AddLocationControl from './components/AddLocationControl';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useTheme } from './theme/useTheme';
import { siteLinks } from './config/siteLinks';
import ChatClientsPage from './pages/ChatClientsPage';
import CurrentAIWeatherPage from './pages/CurrentAIWeatherPage';
import HelloWorldPage from './pages/HelloWorldPage';
import MapPage from './pages/MapPage';
import { MapPinsProvider } from './map/mapPinsContext';
import {
  useLazyGetAboutQuery,
} from './services/weatherApi';

/** Formats a build timestamp like "7/12/2026 10:22:14 PM UTC" (matches MVC and Blazor). */
function formatBuildStart(isoDate) {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) {
    return String(isoDate);
  }

  const month = date.getUTCMonth() + 1;
  const day = date.getUTCDate();
  const year = date.getUTCFullYear();
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  const seconds = String(date.getUTCSeconds()).padStart(2, '0');
  const period = date.getUTCHours() >= 12 ? 'PM' : 'AM';
  const hours = date.getUTCHours() % 12 || 12;

  return `${month}/${day}/${year} ${hours}:${minutes}:${seconds} ${period} UTC`;
}

export function AboutTreeNode({ node }) {
  if (!node) {
    return null;
  }

  const hasChildren = Array.isArray(node.children) && node.children.length > 0;
  const metadata = [];
  if (Number.isFinite(node.buildNumber)) {
    metadata.push({ text: `Build #${node.buildNumber}`, value: node.buildNumber });
  }
  if (node.buildStart) {
    metadata.push({ text: `Started ${formatBuildStart(node.buildStart)}`, value: formatBuildStart(node.buildStart) });
  }
  if (node.buildBranchName) {
    metadata.push({ text: `Branch ${node.buildBranchName}`, value: node.buildBranchName, isBranch: true });
  }

  return (
    <li className="my-2">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-semibold text-foreground">{node.name ?? 'Unnamed node'}</span>
        <span
          className={`rounded-full px-2 py-0.5 text-[0.7rem] font-bold tracking-wide uppercase ${
            node.isHealthy
              ? 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-300'
              : 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300'
          }`}
        >
          {node.isHealthy ? 'Healthy' : 'Unhealthy'}
        </span>
      </div>
      {node.publicMessage && <div className="mt-1 text-xs text-muted-foreground">{node.publicMessage}</div>}
      {metadata.length > 0 && (
        <div className="mt-1 text-xs text-muted-foreground">
          {metadata.map((item, index) => (
            <span key={`${item.text}-${index}`}>
              {index > 0 && ' | '}
              <span className={item.isBranch && item.value !== 'main' ? 'text-amber-600 dark:text-amber-400' : undefined}>
                {item.text}
              </span>
            </span>
          ))}
        </div>
      )}

      {hasChildren && (
        <ul className="mt-1 list-disc pl-5">
          {node.children.map((child, index) => (
            <AboutTreeNode key={`${child.name ?? 'node'}-${index}`} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}

function SiteLinksFooter() {
  return (
    <div className="mt-4 flex flex-wrap gap-x-4 gap-y-3 border-t border-border pt-3 text-sm">
      {siteLinks.map((link) => (
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

function AppShell() {
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const [loadAbout, aboutQuery] = useLazyGetAboutQuery();
  const { preference, setPreference } = useTheme();

  const handleAboutClick = () => {
    setIsAboutOpen(true);
    loadAbout();
  };

  return (
    <div className="flex h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-background shadow-sm">
        <div className="flex w-full flex-wrap items-center justify-between gap-3 px-4 py-3">
          <Link className="flex min-w-0 items-center gap-2 text-inherit no-underline" to="/">
            <img src="/logo.svg" alt="Weather logo" className="h-6 w-6 shrink-0" />
            <h1 className="truncate text-xl font-semibold">Weather React</h1>
          </Link>

          <div className="flex items-center gap-2">
            <AddLocationControl />
            <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Open user menu"
                className="size-9 overflow-hidden rounded-full border-2 border-border bg-background text-muted-foreground hover:bg-muted hover:text-foreground"
              >
                <img src="/avatar.svg" alt="" width="20" height="20" className="avatar-icon block size-5 shrink-0" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="min-w-44">
              <DropdownMenuItem>Login/Logout</DropdownMenuItem>
              <DropdownMenuSeparator />
              {siteLinks.map((link) => (
                <DropdownMenuItem key={link.label} asChild>
                  <a href={link.href} target="_blank" rel="noopener noreferrer">
                    {link.label}
                  </a>
                </DropdownMenuItem>
              ))}
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link to="/hello-world">Hello World</Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link to="/current-ai-weather">Current AI Weather</Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link to="/chat-clients">Chat Clients</Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Theme</DropdownMenuLabel>
              <DropdownMenuRadioGroup value={preference} onValueChange={setPreference}>
                <DropdownMenuRadioItem value="light">Light</DropdownMenuRadioItem>
                <DropdownMenuRadioItem value="dark">Dark</DropdownMenuRadioItem>
                <DropdownMenuRadioItem value="system">System</DropdownMenuRadioItem>
              </DropdownMenuRadioGroup>
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={handleAboutClick}>About</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          </div>
        </div>
      </header>

      <div className="flex min-h-0 flex-1 flex-col">
        <Routes>
          <Route path="/" element={<MapPage />} />
          <Route path="/hello-world" element={<HelloWorldPage />} />
          <Route path="/current-ai-weather" element={<CurrentAIWeatherPage />} />
          <Route path="/chat-clients" element={<ChatClientsPage />} />
        </Routes>
      </div>

      <Dialog open={isAboutOpen} onOpenChange={setIsAboutOpen}>
        <DialogContent className="max-h-[calc(100vh-5rem)] overflow-y-auto sm:max-w-2xl">
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
    </div>
  );
}

function App() {
  return (
    <MapPinsProvider>
      <AppShell />
    </MapPinsProvider>
  );
}

export default App;
