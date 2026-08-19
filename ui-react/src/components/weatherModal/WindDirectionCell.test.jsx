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

test('normalizes wraparound and non-finite degrees for arrow rotation', () => {
  const { rerender } = render(<WindDirectionCell compass="S" degrees={540} />);
  expect(screen.getByText('S (180°)')).toBeDefined();
  expect(screen.getByText(WIND_DIRECTION_ARROW).style.transform).toBe('rotate(180deg)');

  rerender(<WindDirectionCell compass="N" degrees={Number.NaN} />);
  expect(screen.getByText('N (0°)')).toBeDefined();
  expect(screen.getByText(WIND_DIRECTION_ARROW).style.transform).toBe('rotate(0deg)');
});
