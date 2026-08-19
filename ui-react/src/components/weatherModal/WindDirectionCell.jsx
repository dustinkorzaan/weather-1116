import {
  formatWindDirection,
  WIND_DIRECTION_ARROW,
  normalizeSourceDegrees,
} from '../../utils/aiWeatherDisplay';

/** Compass label plus the same rotated letter V used on Current AI Weather. */
function WindDirectionCell({ compass, degrees }) {
  const rotationDeg = normalizeSourceDegrees(degrees);
  return (
    <span className="inline-flex items-center gap-2">
      <span>{formatWindDirection(compass, rotationDeg)}</span>
      <span
        aria-hidden="true"
        className="inline-block origin-center"
        style={{ transform: `rotate(${rotationDeg}deg)` }}
      >
        {WIND_DIRECTION_ARROW}
      </span>
    </span>
  );
}

export default WindDirectionCell;
