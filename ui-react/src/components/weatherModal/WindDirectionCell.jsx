import {
  formatWindDirection,
  WIND_DIRECTION_ARROW,
  windArrowRotationDeg,
} from '../../utils/aiWeatherDisplay';

/** Compass label plus the same rotated ⮛ used on Current AI Weather. */
function WindDirectionCell({ compass, degrees }) {
  const rotationDeg = windArrowRotationDeg(degrees);

  return (
    <span className="inline-flex items-center gap-2">
      <span>{formatWindDirection(compass, degrees)}</span>
      {rotationDeg != null && (
        <span
          aria-hidden="true"
          className="inline-block origin-center text-[1.15em] leading-none"
          style={{ transform: `rotate(${rotationDeg}deg)` }}
        >
          {WIND_DIRECTION_ARROW}
        </span>
      )}
    </span>
  );
}

export default WindDirectionCell;
