import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import SafeGfmMarkdown from '../components/markdown/SafeGfmMarkdown';
import {
  useLazyGetCurrentAIWeatherV3Query,
  useLazyGetCurrentAIWeatherV4Query,
  useLazyGetCurrentAIWeatherV5Query,
} from '../services/weatherApi';
import {
  formatLatLong,
  formatRunLogMs,
  formatRunLogTimestamp,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
  normalizeSourceDegrees,
} from '../utils/aiWeatherDisplay';
import { locationFromSearchParams } from '../utils/currentAiWeatherLocation';

const TAB_CONFIG = [
  { id: 'v3', label: 'V3', description: 'In-process tool loop · Like Foundry Console V3' },
  { id: 'v4', label: 'V4', description: 'Remote MCP tools · Like Foundry Console V4' },
  { id: 'v5', label: 'V5', description: 'Hosted Foundry agent · Like Foundry Console V5' },
];

const DEFAULT_LOCATIONS = { v3: 'Nashville, TN', v4: 'Nashville, TN', v5: 'Nashville, TN' };

/**
 * State (location/result/loading/error per tab) is lifted here rather than kept inside
 * per-tab components: Radix's TabsContent unmounts inactive panels, so a component-local
 * useState would lose its data every time the user switched away and back. Query hooks
 * stay separate per version (not one hook parameterized by version) for ease of learning.
 */
function CurrentAIWeatherPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [activeTab, setActiveTab] = useState('v3');
  const [locations, setLocations] = useState(
    () => ({ ...DEFAULT_LOCATIONS, v3: locationFromSearchParams(searchParams) || DEFAULT_LOCATIONS.v3 })
  );
  const queryHandledRef = useRef(false);

  const [triggerV3, resultV3] = useLazyGetCurrentAIWeatherV3Query();
  const [triggerV4, resultV4] = useLazyGetCurrentAIWeatherV4Query();
  const [triggerV5, resultV5] = useLazyGetCurrentAIWeatherV5Query();

  const triggers = { v3: triggerV3, v4: triggerV4, v5: triggerV5 };
  const results = { v3: resultV3, v4: resultV4, v5: resultV5 };

  const activeConfig = TAB_CONFIG.find((tab) => tab.id === activeTab);
  const location = locations[activeTab];
  const { data, isFetching, isError, error } = results[activeTab];

  const requestWeather = (tabId, rawLocation) => {
    const trimmed = (rawLocation ?? locations[tabId]).trim() || 'Nashville, TN';
    setLocations((previous) => ({ ...previous, [tabId]: trimmed }));
    triggers[tabId](trimmed);
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
    requestWeather('v3', fromQuery);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams, setSearchParams, triggerV3]);

  const onSubmit = (event) => {
    event.preventDefault();
    requestWeather(activeTab, location);
  };

  const errorMessage =
    error && typeof error === 'object' && 'data' in error && error.data?.title
      ? error.data.title
      : 'Unable to load AI weather.';

  return (
    <main className="mx-auto w-full max-w-5xl flex-1 overflow-y-auto p-4">
      <section aria-labelledby="current-ai-weather-heading">
        <h2 id="current-ai-weather-heading" className="mb-3 text-xl font-semibold">
          Current AI Weather
        </h2>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList>
            {TAB_CONFIG.map(({ id, label }) => (
              <TabsTrigger key={id} value={id}>
                {label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
        <p className="mt-2 text-sm text-muted-foreground">{activeConfig.description}</p>

        <form className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-2" onSubmit={onSubmit}>
          <label className="font-semibold" htmlFor="ai-weather-location">
            Location:
          </label>
          <input
            id="ai-weather-location"
            className="min-w-40 max-w-80 flex-1 rounded-md border border-input bg-background px-2.5 py-1.5 text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
            type="text"
            value={location}
            onChange={(event) => setLocations((previous) => ({ ...previous, [activeTab]: event.target.value }))}
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
    </main>
  );
}

export default CurrentAIWeatherPage;
