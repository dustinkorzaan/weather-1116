import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { useGetCurrentAIWeatherQuery } from '../../services/weatherApi';
import {
  formatLatLong,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
  windArrowRotationDeg,
} from '../../utils/aiWeatherDisplay';
import { formatLocationWithLatLong } from '../../utils/currentAiWeatherLocation';

/**
 * Current AI Weather tab content for the weather modal. Copied (not shared) from
 * CurrentAIWeather.jsx's results rendering, minus the location input/button — the
 * demo page at /current-ai-weather has its own design constraints and evolves separately.
 */
function CurrentAIWeatherModalTab({ name, lat, lng }) {
  const locationString = formatLocationWithLatLong(name, lat, lng);
  const { data, isFetching, isError, error, refetch } = useGetCurrentAIWeatherQuery(locationString, {
    skip: !locationString,
  });

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

  const windRotationDeg = windArrowRotationDeg(data?.windDirectionDegrees);

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
                <span>{formatWindDirection(data.windDirection, data.windDirectionDegrees)}</span>
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

export default CurrentAIWeatherModalTab;
