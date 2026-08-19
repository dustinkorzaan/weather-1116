import { formatWindDirection, normalizeSourceDegrees } from '../../utils/aiWeatherDisplay';
import WindDirectionArrow from '../WindDirectionArrow';

/** Compass label plus the same rotated arrow used on Current AI Weather. */
function WindDirectionCell({ compass, degrees }) {
  const rotationDeg = normalizeSourceDegrees(degrees);
  return (
    <span className="inline-flex items-center gap-2">
      <span>{formatWindDirection(compass, rotationDeg)}</span>
      <WindDirectionArrow degrees={degrees} />
    </span>
  );
}

export default WindDirectionCell;
