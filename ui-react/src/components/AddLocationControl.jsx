import { useEffect, useRef, useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cityFromAiWeather } from '../data/mapCities';
import { useMapPins } from '../map/mapPinsContext';
import { useLazyGetCurrentAIWeatherQuery } from '../services/weatherApi';

function AddLocationControl() {
  const { addCity } = useMapPins();
  const [isOpen, setIsOpen] = useState(false);
  const [location, setLocation] = useState('Nashville, TN');
  const [error, setError] = useState('');
  const wrapRef = useRef(null);
  const inputRef = useRef(null);
  const [trigger, { isFetching }] = useLazyGetCurrentAIWeatherQuery();

  useEffect(() => {
    if (isOpen && !isFetching) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [isOpen, isFetching]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    function onPointerDown(event) {
      if (isFetching) {
        return;
      }
      if (wrapRef.current && !wrapRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    }

    function onKeyDown(event) {
      if (event.key === 'Escape' && !isFetching) {
        setIsOpen(false);
      }
    }

    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [isOpen, isFetching]);

  const onSubmit = async (event) => {
    event.preventDefault();
    const trimmed = location.trim() || 'Nashville, TN';
    setLocation(trimmed);
    setError('');

    try {
      const data = await trigger(trimmed).unwrap();
      const city = cityFromAiWeather(trimmed, data);
      if (!city) {
        setError('AI weather did not include map coordinates.');
        return;
      }

      addCity(city);
      setIsOpen(false);
      setLocation('Nashville, TN');
    } catch {
      setError('Unable to load AI weather.');
    }
  };

  return (
    <div ref={wrapRef} className="relative">
      <Button
        type="button"
        variant="outline"
        size="icon"
        aria-label="Add location"
        aria-expanded={isOpen}
        aria-controls="add-location-panel"
        className="size-9 rounded-full border-2 border-border bg-background text-muted-foreground hover:bg-muted hover:text-foreground"
        onClick={() => {
          if (isFetching) {
            setIsOpen(true);
            return;
          }
          setIsOpen((open) => !open);
          setError('');
        }}
      >
        <Plus className="size-5" />
      </Button>

      {isOpen && (
        <form
          id="add-location-panel"
          className="absolute right-0 z-50 mt-2 w-72 rounded-lg bg-popover p-3 text-popover-foreground shadow-md ring-1 ring-foreground/10"
          onSubmit={onSubmit}
        >
          <label className="mb-1.5 block text-sm font-semibold" htmlFor="add-location-input">
            Location
          </label>
          <div className="flex flex-col gap-2">
            <input
              ref={inputRef}
              id="add-location-input"
              className="w-full rounded-md border border-input bg-background px-2.5 py-1.5 text-sm text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
              type="text"
              value={location}
              onChange={(event) => setLocation(event.target.value)}
              disabled={isFetching}
              autoComplete="address-level2"
              placeholder="Nashville, TN"
            />
            <Button
              type="submit"
              size="sm"
              className="bg-primary text-primary-foreground shadow-sm hover:bg-primary/80"
              disabled={isFetching}
              aria-busy={isFetching}
            >
              {isFetching && (
                <span
                  className="size-4 animate-spin rounded-full border-2 border-primary-foreground/40 border-t-primary-foreground"
                  aria-hidden="true"
                />
              )}
              <span>{isFetching ? 'Looking up weather…' : 'Add to map'}</span>
            </Button>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
        </form>
      )}
    </div>
  );
}

export default AddLocationControl;
