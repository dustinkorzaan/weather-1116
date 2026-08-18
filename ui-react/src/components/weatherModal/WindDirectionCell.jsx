import {
  formatWindDirection,
  WIND_DIRECTION_ARROW,
} from '../../utils/aiWeatherDisplay';

/** Compass label plus the same rotated ⮛ used on Current AI Weather. */
function WindDirectionCell({ compass, degrees }) {
  return (
    <span className="inline-flex items-center gap-2">
      <span>{formatWindDirection(compass, degrees)}</span>
      <span
        aria-hidden="true"
        className="inline-block origin-center text-[1.15em] leading-none"
        style={{ transform: `rotate(${degrees}deg)` }}
      >
        {WIND_DIRECTION_ARROW}
      </span>
    </span>
  );
}

export default WindDirectionCell;
