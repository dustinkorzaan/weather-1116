import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import WindDirectionCell from './WindDirectionCell';

function getArrow(container) {
  return container.querySelector('svg[aria-hidden="true"]');
}

test('renders compass label and rotates arrow by source degrees', () => {
  const { container } = render(<WindDirectionCell compass="SW" degrees={224} />);

  expect(screen.getByText('SW (224°)')).toBeDefined();
  expect(getArrow(container).style.transform).toBe('rotate(224deg)');
});

test('normalizes wraparound and non-finite degrees for arrow rotation', () => {
  const { container, rerender } = render(<WindDirectionCell compass="S" degrees={540} />);
  expect(screen.getByText('S (180°)')).toBeDefined();
  expect(getArrow(container).style.transform).toBe('rotate(180deg)');

  rerender(<WindDirectionCell compass="N" degrees={Number.NaN} />);
  expect(screen.getByText('N (0°)')).toBeDefined();
  expect(getArrow(container).style.transform).toBe('rotate(0deg)');
});
