import { normalizeSourceDegrees } from '../utils/windDirectionDisplay';

/** Down-pointing filled arrowhead. SVG instead of U+2B9B, which many mobile fonts lack. At 0° it points south (wind from north). */
function WindDirectionArrow({ degrees }) {
  const rotationDeg = normalizeSourceDegrees(degrees);
  return (
    <svg
      aria-hidden="true"
      className="inline-block origin-center shrink-0"
      width="1.15em"
      height="1.15em"
      viewBox="0 0 12 12"
      style={{ transform: `rotate(${rotationDeg}deg)` }}
    >
      <path fill="currentColor" d="M6 11 1.2 2.5h9.6Z" />
    </svg>
  );
}

export default WindDirectionArrow;
