import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { useGetCurrentAIWeatherV4Query } from '../../services/weatherApi';
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
import { formatLocationWithLatLong } from '../../utils/currentAiWeatherLocation';

/**
 * Current AI Weather tab content for the weather modal. Copied (not shared) from
 * CurrentAIWeather.jsx's results rendering, minus the location input/button — the
 * demo page at /current-ai-weather has its own design constraints and evolves separately.
 */
function CurrentAIWeatherModalTab({ name, lat, lng }) {
  const locationString = formatLocationWithLatLong(name, lat, lng);
  const { data, isFetching, isError, error, refetch } = useGetCurrentAIWeatherV4Query(locationString, {
    skip: !locationString,
  });

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

  return (
    <section aria-labelledby="current-ai-weather-modal-heading">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="current-ai-weather-modal-heading" className="text-xl font-semibold">
          Current AI Weather
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => refetch()}
          disabled={isFetching}
          aria-label="Refresh Current AI Weather"
        >
          <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
        </Button>
      </div>

      {isError && <p className="mt-2 text-destructive">{errorMessage}</p>}

      {isFetching && (
        <p className="mt-2 inline-flex items-center gap-2 text-muted-foreground">
          <span
            className="size-4 animate-spin rounded-full border-2 border-border border-t-foreground"
            aria-hidden="true"
          />
          <span>Connecting to Microsoft Foundry...</span>
        </p>
      )}

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

export default CurrentAIWeatherModalTab;
