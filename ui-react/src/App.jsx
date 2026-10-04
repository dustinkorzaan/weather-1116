import { useCallback, useEffect, useState } from 'react';
import { MessageSquareIcon } from 'lucide-react';
import { Link, Route, Routes, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import AddLocationControl from './components/AddLocationControl';
import AboutDialog from './components/about/AboutDialog';
import Chat5aSidebar from './components/chat/Chat5aSidebar';
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
import ChatClientsPage from './pages/ChatClientsPage';
import CurrentAIWeatherPage from './pages/CurrentAIWeatherPage';
import HelloWorldPage from './pages/HelloWorldPage';
import MapPage from './pages/MapPage';
import WeatherModalPage from './pages/WeatherModalPage';
import { MapPinsProvider } from './map/mapPinsContext';

// The logo.svg sun, inlined so it can take the palette's sun color in both themes.
function SunMark() {
  return (
    <svg
      role="img"
      aria-label="Weather logo"
      viewBox="0 0 24 24"
      className="size-7 shrink-0 text-sun"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M12 7c2.76 0 5 2.24 5 5c0 2.76 -2.24 5 -5 5c-2.76 0 -5 -2.24 -5 -5c0 -2.76 2.24 -5 5 -5Z" />
      <path d="M12 21v1M21 12h1M12 3v-1M3 12h-1" />
      <path d="M18.5 18.5l0.5 0.5M18.5 5.5l0.5 -0.5M5.5 5.5l-0.5 -0.5M5.5 18.5l-0.5 0.5" />
    </svg>
  );
}

function AppShell() {
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const { preference, setPreference } = useTheme();
  const { pathname } = useLocation();
  const isMapVisible = pathname === '/';
  const [isChatOpen, setIsChatOpen] = useState(false);
  const closeChat = useCallback(() => setIsChatOpen(false), []);

  // Leaving Home closes the sidebar (history is kept), matching Blazor and MVC.
  useEffect(() => {
    if (!isMapVisible) {
      setIsChatOpen(false);
    }
  }, [isMapVisible]);

  return (
    <div className="flex h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-background">
        <div className="flex w-full flex-wrap items-center justify-between gap-3 px-4 py-2.5">
          <Link
            className="flex min-w-0 items-center gap-2.5 rounded-md text-inherit no-underline focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
            to="/"
          >
            <SunMark />
            <h1 className="truncate text-xl font-semibold">Weather React</h1>
          </Link>

          <div className="flex items-center gap-2">
            {isMapVisible && <AddLocationControl />}
            {isMapVisible && (
              <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Open chat"
                title="Open chat"
                aria-expanded={isChatOpen}
                aria-controls="chat5a-sidebar"
                onClick={() => setIsChatOpen((current) => !current)}
                className="size-9 rounded-full border-2 border-border bg-background text-muted-foreground hover:bg-muted hover:text-foreground"
              >
                <MessageSquareIcon aria-hidden="true" className="size-5" />
              </Button>
            )}
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
              <DropdownMenuItem asChild>
                <Link to="/">Home</Link>
              </DropdownMenuItem>
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
              <DropdownMenuItem onSelect={() => setIsAboutOpen(true)}>About</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          </div>
        </div>
      </header>

      {/* On Home the Chat5a sidebar docks beside the map (under it below 640px) so the map
          shrinks instead of being covered. It stays mounted off Home to keep its history. */}
      <div className="flex min-h-0 flex-1 flex-col sm:flex-row">
        <div className="flex min-h-0 min-w-0 flex-1 flex-col">
          <Routes>
            <Route path="/" element={<MapPage />} />
            <Route path="/hello-world" element={<HelloWorldPage />} />
            <Route path="/current-ai-weather" element={<CurrentAIWeatherPage />} />
            <Route path="/chat-clients" element={<ChatClientsPage />} />
            <Route path="/weather" element={<WeatherModalPage />} />
          </Routes>
        </div>

        <Chat5aSidebar open={isChatOpen && isMapVisible} onClose={closeChat} />
      </div>

      <AboutDialog open={isAboutOpen} onOpenChange={setIsAboutOpen} />
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
