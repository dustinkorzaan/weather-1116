import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useGetForecastQuery } from '../../services/weatherApi';
import { formatWindDirection } from '../../utils/aiWeatherDisplay';
import { degreesToCompass, formatClockTime, formatPrecipitationMm, formatTemperatureC, formatWindSpeedKmh } from '../../utils/weatherGridFormat';

/** Static single-use grid for the Every 15 Forecast tab — soonest first. */
function Every15ForecastTab({ lat, lng }) {
  const { data, isFetching, isError, refetch } = useGetForecastQuery(
    { latitude: lat, longitude: lng, resolution: 'FifteenMinutes' },
    { skip: lat == null || lng == null }
  );

  const minutely15 = data?.minutely_15;
  const rows = (minutely15?.time ?? []).map((time, index) => ({
    time,
    temperature: minutely15.temperature_2m[index],
    precipitation: minutely15.precipitation[index],
    windSpeed: minutely15.wind_speed_10m[index],
    windDirection: minutely15.wind_direction_10m[index],
  }));

  return (
    <section aria-labelledby="every-15-forecast-heading">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="every-15-forecast-heading" className="text-xl font-semibold">
          Every 15 Forecast
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => refetch()}
          disabled={isFetching}
          aria-label="Refresh Every 15 Forecast"
        >
          <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
        </Button>
      </div>

      {isError && <p className="text-destructive">Unable to load 15-minute forecast.</p>}

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

export default Every15ForecastTab;
