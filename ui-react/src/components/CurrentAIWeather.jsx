import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import SafeGfmMarkdown from './markdown/SafeGfmMarkdown';
import { useLazyGetCurrentAIWeatherQuery } from '../services/weatherApi';
import {
  formatLatLong,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
  windArrowRotationDeg,
} from '../utils/aiWeatherDisplay';
import { locationFromSearchParams } from '../utils/currentAiWeatherLocation';

function CurrentAIWeather() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [location, setLocation] = useState(
    () => locationFromSearchParams(searchParams) || 'Nashville, TN'
  );
  const [trigger, { data, isFetching, isError, error }] = useLazyGetCurrentAIWeatherQuery();
  const queryHandledRef = useRef(false);

  const requestWeather = (rawLocation) => {
    const trimmed = (rawLocation ?? location).trim() || 'Nashville, TN';
    setLocation(trimmed);
    trigger(trimmed);
  };

  useEffect(() => {
    if (queryHandledRef.current) {
      return;
    }

    const fromQuery = locationFromSearchParams(searchParams);
    if (!fromQuery) {
      return;
    }

    queryHandledRef.current = true;
    setSearchParams({}, { replace: true });
    requestWeather(fromQuery);
  }, [searchParams, setSearchParams, trigger]);

  const onSubmit = (event) => {
    event.preventDefault();
    requestWeather(location);
  };

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

  const windRotationDeg = windArrowRotationDeg(data?.windDirectionFromDegrees);

  return (
    <section aria-labelledby="current-ai-weather-heading">
      <h2 id="current-ai-weather-heading" className="mb-3 text-xl font-semibold">
        Current AI Weather
      </h2>
      <form className="flex flex-wrap items-center gap-x-3 gap-y-2" onSubmit={onSubmit}>
        <label className="font-semibold" htmlFor="ai-weather-location">
          Location:
        </label>
        <input
          id="ai-weather-location"
          className="min-w-40 max-w-80 flex-1 rounded-md border border-input bg-background px-2.5 py-1.5 text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
          type="text"
          value={location}
          onChange={(event) => setLocation(event.target.value)}
          disabled={isFetching}
          autoComplete="address-level2"
        />
        <Button
          type="submit"
          size="lg"
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
          <span>Get Current AI Weather</span>
        </Button>
      </form>

      {isError && <p className="mt-2 text-destructive">{errorMessage}</p>}

      {data && !isFetching && (
        <div className="mt-3.5">
          <div className="chat-markdown mb-2.5 text-base">
            <SafeGfmMarkdown>{data.fullSummary}</SafeGfmMarkdown>
          </div>
          <dl className="grid gap-x-4 gap-y-1.5">
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Temperature</dt>
              <dd>{formatTemperatureF(data.temperatureF)}</dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Wind Speed</dt>
              <dd>{formatWindSpeedMph(data.windSpeedMPH)}</dd>
            </div>
            <div className="grid grid-cols-1 items-center gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Wind Direction</dt>
              <dd className="inline-flex items-center gap-2">
                <span>{formatWindDirection(data.windDirection, data.windDirectionFromDegrees)}</span>
                {windRotationDeg != null && (
                  <span
                    aria-hidden="true"
                    className="inline-block origin-center text-[1.15em] leading-none"
                    style={{ transform: `rotate(${windRotationDeg}deg)` }}
                  >
                    {WIND_DIRECTION_ARROW}
                  </span>
                )}
              </dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Conditions</dt>
              <dd>{data.conditions}</dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Lat/Long</dt>
              <dd>{formatLatLong(data.latitude, data.longitude)}</dd>
            </div>
          </dl>
        </div>
      )}
    </section>
  );
}

export default CurrentAIWeather;
