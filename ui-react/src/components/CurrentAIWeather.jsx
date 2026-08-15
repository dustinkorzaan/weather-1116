import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useLazyGetCurrentAIWeatherQuery } from '../services/weatherApi';

function CurrentAIWeather() {
  const [location, setLocation] = useState('Nashville, TN');
  const [trigger, { data, isFetching, isError, error }] = useLazyGetCurrentAIWeatherQuery();

  const onSubmit = (event) => {
    event.preventDefault();
    const trimmed = location.trim() || 'Nashville, TN';
    trigger(trimmed);
  };

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

  return (
    <section className="mt-5" aria-label="Current AI weather">
      <form className="flex flex-wrap items-center gap-x-3 gap-y-2" onSubmit={onSubmit}>
        <label className="font-semibold" htmlFor="ai-weather-location">
          Location:
        </label>
        <input
          id="ai-weather-location"
          className="min-w-40 max-w-80 flex-1 rounded-md border border-gray-300 px-2.5 py-1.5 focus:border-gray-800 focus:outline-none disabled:bg-gray-100"
          type="text"
          value={location}
          onChange={(event) => setLocation(event.target.value)}
          disabled={isFetching}
          autoComplete="address-level2"
        />
        <Button
          type="submit"
          variant="outline"
          size="lg"
          className="border-gray-300 bg-white text-gray-800 hover:bg-gray-50"
          disabled={isFetching}
          aria-busy={isFetching}
        >
          {isFetching && (
            <span
              className="size-4 animate-spin rounded-full border-2 border-gray-200 border-t-gray-600"
              aria-hidden="true"
            />
          )}
          <span>Get Current AI Weather</span>
        </Button>
      </form>

      {isError && <p className="mt-2 text-red-700">{errorMessage}</p>}

      {data && !isFetching && (
        <div className="mt-3.5">
          <p className="mb-2.5 text-base">{data.fullSummary}</p>
          <dl className="grid gap-x-4 gap-y-1.5">
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Temperature F</dt>
              <dd>{data.temperatureF}</dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Wind Speed MPH</dt>
              <dd>{data.windSpeedMPH}</dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Wind Direction</dt>
              <dd>{data.windDirection}</dd>
            </div>
            <div className="grid grid-cols-1 items-baseline gap-2 sm:grid-cols-[minmax(8rem,11rem)_1fr]">
              <dt className="font-semibold">Conditions</dt>
              <dd>{data.conditions}</dd>
            </div>
          </dl>
        </div>
      )}
    </section>
  );
}

export default CurrentAIWeather;
