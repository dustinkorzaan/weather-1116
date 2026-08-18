import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useGetHistoryQuery } from '../../services/weatherApi';
import { formatWindDirection } from '../../utils/aiWeatherDisplay';
import { degreesToCompass, formatClockTime, formatPrecipitationMm, formatTemperatureC, formatWindSpeedKmh } from '../../utils/weatherGridFormat';

/** Static single-use grid for the Hourly History tab — most recent first. */
function HourlyHistoryTab({ lat, lng }) {
  const { data, isFetching, isError, refetch } = useGetHistoryQuery(
    { latitude: lat, longitude: lng, resolution: 'Hourly' },
    { skip: lat == null || lng == null }
  );

  const hourly = data?.hourly;
  const rows = (hourly?.time ?? [])
    .map((time, index) => ({
      time,
      temperature: hourly.temperature_2m[index],
      precipitation: hourly.precipitation[index],
      windSpeed: hourly.wind_speed_10m[index],
      windDirection: hourly.wind_direction_10m[index],
    }))
    .reverse();

  return (
    <section aria-labelledby="hourly-history-heading">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="hourly-history-heading" className="text-xl font-semibold">
          Hourly History
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => refetch()}
          disabled={isFetching}
          aria-label="Refresh Hourly History"
        >
          <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
        </Button>
      </div>

      {isError && <p className="text-destructive">Unable to load hourly history.</p>}

      {rows.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-sm">
            <thead>
              <tr className="border-b border-border text-muted-foreground">
                <th className="py-1.5 pr-4 font-semibold">Time</th>
                <th className="py-1.5 pr-4 font-semibold">Temp</th>
                <th className="py-1.5 pr-4 font-semibold">Precip</th>
                <th className="py-1.5 pr-4 font-semibold">Wind Speed</th>
                <th className="py-1.5 font-semibold">Wind Direction</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.time} className="border-b border-border/50">
                  <td className="py-1.5 pr-4">{formatClockTime(row.time)}</td>
                  <td className="py-1.5 pr-4">{formatTemperatureC(row.temperature)}</td>
                  <td className="py-1.5 pr-4">{formatPrecipitationMm(row.precipitation)}</td>
                  <td className="py-1.5 pr-4">{formatWindSpeedKmh(row.windSpeed)}</td>
                  <td className="py-1.5">
                    {formatWindDirection(degreesToCompass(row.windDirection), row.windDirection)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export default HourlyHistoryTab;
