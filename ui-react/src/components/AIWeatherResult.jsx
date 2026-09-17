import SafeGfmMarkdown from './markdown/SafeGfmMarkdown';
import {
  formatLatLong,
  formatRunLogMs,
  formatRunLogTimestamp,
  formatRunLogTokenCount,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
  normalizeSourceDegrees,
} from '../utils/aiWeatherDisplay';

/** Renders an AI weather query result: summary markdown, a stat grid, and an optional run-log table. */
function AIWeatherResult({ data }) {
  return (
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
                <th className="py-1.5 pr-4 font-semibold">Input (tokens)</th>
                <th className="py-1.5 pr-4 font-semibold">Cached (tokens)</th>
                <th className="py-1.5 pr-4 font-semibold">Output (tokens)</th>
                <th className="py-1.5 pr-4 font-semibold">Reasoning (tokens)</th>
                <th className="py-1.5 pr-4 font-semibold">Total (tokens)</th>
                <th className="py-1.5 pr-4 font-semibold">Running Total (tokens)</th>
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
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.inputTokenCount)}</td>
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.cachedTokenCount)}</td>
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.outputTokenCount)}</td>
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.reasoningTokenCount)}</td>
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.totalTokenCount)}</td>
                  <td className="py-1.5 pr-4">{formatRunLogTokenCount(entry.runningTotalTokenCount)}</td>
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
  );
}

export default AIWeatherResult;
