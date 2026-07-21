import { useRef, useState } from 'react';
import { streamCurrentAIWeather } from '../services/streamCurrentAIWeather';

function CurrentAIWeather() {
  const [location, setLocation] = useState('Nashville, TN');
  const [data, setData] = useState(null);
  const [isFetching, setIsFetching] = useState(false);
  const [isError, setIsError] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [statusMessage, setStatusMessage] = useState('');
  const [streamPreview, setStreamPreview] = useState('');
  const abortRef = useRef(null);

  const onSubmit = async (event) => {
    event.preventDefault();
    abortRef.current?.abort();

    const trimmed = location.trim() || 'Nashville, TN';
    const controller = new AbortController();
    abortRef.current = controller;

    setLocation(trimmed);
    setIsFetching(true);
    setIsError(false);
    setErrorMessage('');
    setStatusMessage('Starting AI weather request...');
    setStreamPreview('');
    setData(null);

    try {
      await streamCurrentAIWeather(trimmed, {
        signal: controller.signal,
        onUpdate: (update) => {
          if (update.type === 'status' && update.message) {
            setStatusMessage(update.message);
          }

          if (update.type === 'textDelta' && update.delta) {
            setStreamPreview((current) => current + update.delta);
          }

          if (update.type === 'complete' && update.result) {
            setData(update.result);
            setStatusMessage('');
            setStreamPreview('');
          }
        },
      });
    } catch (error) {
      if (error.name !== 'AbortError') {
        setIsError(true);
        setErrorMessage(error.message || 'Unable to load AI weather.');
        setStatusMessage('');
        setStreamPreview('');
      }
    } finally {
      if (abortRef.current === controller) {
        setIsFetching(false);
        abortRef.current = null;
      }
    }
  };

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

      {isFetching && statusMessage && (
        <p className="ai-weather-status" aria-live="polite">
          {statusMessage}
        </p>
      )}

      {isFetching && streamPreview && (
        <pre className="ai-weather-stream-preview" aria-live="polite">
          {streamPreview}
        </pre>
      )}

      {isError && <p className="forecast-status error">{errorMessage}</p>}

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
