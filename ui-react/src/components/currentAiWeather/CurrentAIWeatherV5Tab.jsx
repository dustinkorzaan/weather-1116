import { useState } from 'react';
import { Button } from '@/components/ui/button';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { useLazyGetCurrentAIWeatherV5Query } from '../../services/weatherApi';
import {
  formatLatLong,
  formatRunLogMs,
  formatRunLogTimestamp,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
  normalizeSourceDegrees,
} from '../../utils/aiWeatherDisplay';

/**
 * V5 tab: hosted Microsoft Foundry agent — instructions, response schema, and
 * MCP tools are configured on the agent itself (like Foundry Console V5).
 * Copied (not shared) from the V3/V4 tabs — this repo duplicates each AI
 * weather variant rather than parameterizing one generic component.
 */
function CurrentAIWeatherV5Tab() {
  const [location, setLocation] = useState('Nashville, TN');
  const [trigger, { data, isFetching, isError, error }] = useLazyGetCurrentAIWeatherV5Query();

  const requestWeather = (rawLocation) => {
    const trimmed = (rawLocation ?? location).trim() || 'Nashville, TN';
    setLocation(trimmed);
    trigger(trimmed);
  };

  const onSubmit = (event) => {
    event.preventDefault();
    requestWeather(location);
  };

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

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
                <span>{formatWindDirection(data.windDirectionSource, data.windDirectionSourceDegrees)}</span>
                <span
                  aria-hidden="true"
                  className="inline-block origin-center"
                  style={{ transform: `rotate(${normalizeSourceDegrees(data.windDirectionSourceDegrees)}deg)` }}
                >
                  {WIND_DIRECTION_ARROW}
                </span>
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

          {data.runLogDetails?.length > 0 && (
            <div className="mt-4 overflow-x-auto">
              <table className="w-full border-collapse text-left text-sm">
                <thead>
                  <tr className="border-b border-border text-muted-foreground">
                    <th className="py-1.5 pr-4 font-semibold">Time (UTC)</th>
                    <th className="py-1.5 pr-4 font-semibold">Loop</th>
                    <th className="py-1.5 pr-4 font-semibold">Message</th>
                    <th className="py-1.5 pr-4 font-semibold">Input</th>
                    <th className="py-1.5 pr-4 font-semibold">Cached</th>
                    <th className="py-1.5 pr-4 font-semibold">Output</th>
                    <th className="py-1.5 pr-4 font-semibold">Reasoning</th>
                    <th className="py-1.5 pr-4 font-semibold">Total</th>
                    <th className="py-1.5 pr-4 font-semibold">Runtime (ms)</th>
                    <th className="py-1.5 pr-4 font-semibold">Loop Runtime (ms)</th>
                    <th className="py-1.5 font-semibold">Running Total (ms)</th>
                  </tr>
                </thead>
                <tbody>
                  {data.runLogDetails.map((entry, index) => (
                    <tr key={index} className="border-b border-border/50">
                      <td className="py-1.5 pr-4">{formatRunLogTimestamp(entry.dateTimeUtc)}</td>
                      <td className="py-1.5 pr-4">{entry.loopNumber}</td>
                      <td className="py-1.5 pr-4">{entry.message}</td>
                      <td className="py-1.5 pr-4">{entry.inputTokenCount ?? ''}</td>
                      <td className="py-1.5 pr-4">{entry.cachedTokenCount ?? ''}</td>
                      <td className="py-1.5 pr-4">{entry.outputTokenCount ?? ''}</td>
                      <td className="py-1.5 pr-4">{entry.reasoningTokenCount ?? ''}</td>
                      <td className="py-1.5 pr-4">{entry.totalTokenCount ?? ''}</td>
                      <td className="py-1.5 pr-4">{formatRunLogMs(entry.runtimeMs)}</td>
                      <td className="py-1.5 pr-4">{formatRunLogMs(entry.loopRuntimeMs)}</td>
                      <td className="py-1.5">{formatRunLogMs(entry.runningTotalMs)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <p className="mt-1.5 text-sm text-muted-foreground">
                Total Runtime: {formatRunLogMs(data.runLogDetails[data.runLogDetails.length - 1].runningTotalMs)} ms
              </p>
            </div>
          )}
        </div>
      )}
    </section>
  );
}

export default CurrentAIWeatherV5Tab;
