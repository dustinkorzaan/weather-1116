import {
  formatWindDirection,
  WIND_DIRECTION_ARROW,
} from '../../utils/aiWeatherDisplay';
import { degreesToCompass } from '../../utils/weatherGridFormat';

/** Compass label plus the same rotated ⮛ used on Current AI Weather. */
function WindDirectionCell({ degrees }) {
  const numeric = Number(degrees);
  const rotationDeg = Number.isFinite(numeric)
    ? Math.round(((numeric % 360) + 360) % 360)
    : null;

  return (
    <span className="inline-flex items-center gap-2">
      <span>{formatWindDirection(degreesToCompass(degrees), degrees)}</span>
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
