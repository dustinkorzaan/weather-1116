import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import BackendWakeScreen from './BackendWakeScreen';

test('puts the heading before the target rows and the please-wait line after them', () => {
  render(
    <BackendWakeScreen>
      <div data-testid="target-row">a target row</div>
    </BackendWakeScreen>
  );

  const heading = screen.getByText('Waking up the weather services…');
  const targetRow = screen.getByTestId('target-row');
  const footer = screen.getByText('Please wait, this can take 30 - 60 seconds…');

  expect(heading.compareDocumentPosition(targetRow) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(targetRow.compareDocumentPosition(footer) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
});
