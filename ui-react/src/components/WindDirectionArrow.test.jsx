import { expect, test } from 'vitest';
import { render } from '@testing-library/react';
import WindDirectionArrow from './WindDirectionArrow';

test('rotates the SVG arrowhead by normalized source degrees', () => {
  const { container, rerender } = render(<WindDirectionArrow degrees={224} />);
  const arrow = container.querySelector('svg[aria-hidden="true"]');
  expect(arrow).not.toBeNull();
  expect(arrow.style.transform).toBe('rotate(224deg)');
  expect(arrow.querySelector('path')?.getAttribute('d')).toBe('M6 11 1.2 2.5h9.6Z');

  rerender(<WindDirectionArrow degrees={540} />);
  expect(container.querySelector('svg[aria-hidden="true"]').style.transform).toBe('rotate(180deg)');

  rerender(<WindDirectionArrow degrees={Number.NaN} />);
  expect(container.querySelector('svg[aria-hidden="true"]').style.transform).toBe('rotate(0deg)');
});
