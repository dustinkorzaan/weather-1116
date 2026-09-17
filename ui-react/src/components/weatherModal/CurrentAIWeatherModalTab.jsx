import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import AIWeatherResult from '../AIWeatherResult';
import { useGetCurrentAIWeatherV3Query } from '../../services/weatherApi';
import { formatLocationWithLatLong } from '../../utils/currentAiWeatherLocation';

/** Current AI Weather tab content for the weather modal, minus the location input/button. */
function CurrentAIWeatherModalTab({ name, lat, lng }) {
  const locationString = formatLocationWithLatLong(name, lat, lng);
  const { data, isFetching, isError, error, refetch } = useGetCurrentAIWeatherV3Query(locationString, {
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

      {data && !isFetching && <AIWeatherResult data={data} />}
    </section>
  );
}

export default CurrentAIWeatherModalTab;
