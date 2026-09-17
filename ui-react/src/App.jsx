import { useState } from 'react';
import { Link, Route, Routes, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import AddLocationControl from './components/AddLocationControl';
import AboutDialog from './components/about/AboutDialog';
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

function AppShell() {
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const { preference, setPreference } = useTheme();
  const { pathname } = useLocation();
  const isMapVisible = pathname === '/';

  return (
    <div className="flex h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-background shadow-sm">
        <div className="flex w-full flex-wrap items-center justify-between gap-3 px-4 py-3">
          <Link className="flex min-w-0 items-center gap-2 text-inherit no-underline" to="/">
            <img src="/logo.svg" alt="Weather logo" className="h-6 w-6 shrink-0" />
            <h1 className="truncate text-xl font-semibold">Weather React</h1>
          </Link>

          <div className="flex items-center gap-2">
            {isMapVisible && <AddLocationControl />}
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

      <div className="flex min-h-0 flex-1 flex-col">
        <Routes>
          <Route path="/" element={<MapPage />} />
          <Route path="/hello-world" element={<HelloWorldPage />} />
          <Route path="/current-ai-weather" element={<CurrentAIWeatherPage />} />
          <Route path="/chat-clients" element={<ChatClientsPage />} />
          <Route path="/weather" element={<WeatherModalPage />} />
        </Routes>
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
