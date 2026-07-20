import CurrentAIWeather from '../components/CurrentAIWeather';
import WeatherMap from '../components/WeatherMap';
import {
  useGetForecastQuery,
  useGetHelloQuery,
} from '../services/weatherApi';

/** Formats an API date-only string (yyyy-MM-dd) in local time, matching .NET ToShortDateString(). */
function formatForecastDate(isoDate) {
  const datePart = String(isoDate).split('T')[0];
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) {
    return datePart;
  }
  return new Date(year, month - 1, day).toLocaleDateString();
}

function kelvinToC(kelvin) {
  return Number.isFinite(kelvin) ? kelvin - 273.15 : NaN;
}

function kelvinToF(kelvin) {
  return Number.isFinite(kelvin) ? ((kelvin - 273.15) * 9) / 5 + 32 : NaN;
}

function formatTemp(value) {
  return Number.isFinite(value) ? value.toFixed(2) : 'N/A';
}

function HomePage() {
  const { data: helloMessage, isError: isHelloError } = useGetHelloQuery();
  const {
    data: forecasts,
    isLoading: isForecastLoading,
    isError: isForecastError,
  } = useGetForecastQuery();

  return (
    <main className="home-content">
      <p className="hello-message">
        {isHelloError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
      </p>

      <CurrentAIWeather />

      <WeatherMap />

      <h2 className="forecast-title">Weather forecast</h2>

      {isForecastLoading && <p className="forecast-status">Loading...</p>}
      {isForecastError && <p className="forecast-status error">Unable to load weather forecast from API.</p>}
      {forecasts && (
        <div className="table-responsive">
          <table className="forecast-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
              </tr>
            </thead>
            <tbody>
              {forecasts.map((forecast) => (
                <tr key={forecast.date}>
                  <td>{formatForecastDate(forecast.date)}</td>
                  <td>{formatTemp(kelvinToC(forecast.temperatureK))}</td>
                  <td>{formatTemp(kelvinToF(forecast.temperatureK))}</td>
                  <td>{forecast.summary}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}

export default HomePage;
