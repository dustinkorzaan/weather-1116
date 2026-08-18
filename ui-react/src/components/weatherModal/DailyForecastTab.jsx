import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useGetForecastQuery } from '../../services/weatherApi';
import WindDirectionCell from './WindDirectionCell';
import { formatCalendarDate, formatPrecipitationIn, formatTemperatureF, formatWindSpeedMph } from '../../utils/weatherGridFormat';

/** Static single-use grid for the Daily Forecast tab — soonest first. */
function DailyForecastTab({ lat, lng }) {
  const { data, isFetching, isError, refetch } = useGetForecastQuery(
    { latitude: lat, longitude: lng, resolution: 'Daily' },
    { skip: lat == null || lng == null }
  );

  const daily = data?.daily;
  const rows = (daily?.time ?? []).map((time, index) => ({
    time,
    high: daily.temperatureHighF[index],
    low: daily.temperatureLowF[index],
    precipitation: daily.precipitationInch[index],
    windSpeed: daily.windSpeedMPH[index],
    windDirection: daily.windDirectionSourceDegrees[index],
  }));

  return (
    <section aria-labelledby="daily-forecast-heading">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="daily-forecast-heading" className="text-xl font-semibold">
          Daily Forecast
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => refetch()}
          disabled={isFetching}
          aria-label="Refresh Daily Forecast"
        >
          <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
        </Button>
      </div>

      {isError && <p className="text-destructive">Unable to load daily forecast.</p>}

      {rows.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-sm">
            <thead>
              <tr className="border-b border-border text-muted-foreground">
                <th className="py-1.5 pr-4 font-semibold">Date</th>
                <th className="py-1.5 pr-4 font-semibold">High</th>
                <th className="py-1.5 pr-4 font-semibold">Low</th>
                <th className="py-1.5 pr-4 font-semibold">Precip</th>
                <th className="py-1.5 pr-4 font-semibold">Wind Speed</th>
                <th className="py-1.5 font-semibold">Wind Direction</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.time} className="border-b border-border/50">
                  <td className="py-1.5 pr-4">{formatCalendarDate(row.time)}</td>
                  <td className="py-1.5 pr-4">{formatTemperatureF(row.high)}</td>
                  <td className="py-1.5 pr-4">{formatTemperatureF(row.low)}</td>
                  <td className="py-1.5 pr-4">{formatPrecipitationIn(row.precipitation)}</td>
                  <td className="py-1.5 pr-4">{formatWindSpeedMph(row.windSpeed)}</td>
                  <td className="py-1.5">
                    <WindDirectionCell degrees={row.windDirection} />
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

export default DailyForecastTab;
