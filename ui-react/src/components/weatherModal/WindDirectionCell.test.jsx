import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import WindDirectionCell from './WindDirectionCell';
import { WIND_DIRECTION_ARROW } from '../../utils/aiWeatherDisplay';

test('renders compass label and rotates arrow by source degrees', () => {
  render(<WindDirectionCell compass="SW" degrees={224} />);

  expect(screen.getByText('SW (224°)')).toBeDefined();
  const arrow = screen.getByText(WIND_DIRECTION_ARROW);
  expect(arrow.style.transform).toBe('rotate(224deg)');
});
