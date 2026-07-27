import { useState } from 'react';
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
    <section className="ai-weather-section" aria-label="Current AI weather">
      <form className="ai-weather-form" onSubmit={onSubmit}>
        <label className="ai-weather-label" htmlFor="ai-weather-location">
          Location:
        </label>
        <input
          id="ai-weather-location"
          className="ai-weather-input"
          type="text"
          value={location}
          onChange={(event) => setLocation(event.target.value)}
          disabled={isFetching}
          autoComplete="address-level2"
        />
        <button
          type="submit"
          className="ai-weather-button"
          disabled={isFetching}
          aria-busy={isFetching}
        >
          {isFetching && <span className="ai-weather-spinner" aria-hidden="true" />}
          <span>Get Current AI Weather</span>
        </button>
      </form>

      {isError && <p className="status-message error">{errorMessage}</p>}

      {data && !isFetching && (
        <div className="ai-weather-results">
          <p className="ai-weather-summary">{data.fullSummary}</p>
          <dl className="ai-weather-stats">
            <div className="ai-weather-stat">
              <dt>Temperature F</dt>
              <dd>{data.temperatureF}</dd>
            </div>
            <div className="ai-weather-stat">
              <dt>Wind Speed MPH</dt>
              <dd>{data.windSpeedMPH}</dd>
            </div>
            <div className="ai-weather-stat">
              <dt>Wind Direction</dt>
              <dd>{data.windDirection}</dd>
            </div>
            <div className="ai-weather-stat">
              <dt>Conditions</dt>
              <dd>{data.conditions}</dd>
            </div>
          </dl>
        </div>
      )}
    </section>
  );
}

export default CurrentAIWeather;
