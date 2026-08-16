import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import ChatMarkdown from './ChatMarkdown';

test('renders GFM tables and emphasis after the markdown is complete', () => {
  render(
    <ChatMarkdown>
      {`**Warmest** today:

| City | Temp |
| --- | --- |
| Nashville | 72 |
| Atlanta | 80 |
`}
    </ChatMarkdown>
  );

  expect(screen.getByRole('table')).toBeDefined();
  expect(screen.getByText('Nashville')).toBeDefined();
  expect(screen.getByText('Atlanta')).toBeDefined();
  expect(screen.getByText('Warmest').tagName).toBe('STRONG');
});

test('does not render raw HTML from the model', () => {
  render(
    <ChatMarkdown>{'Hello <script>alert(1)</script> world'}</ChatMarkdown>
  );

  expect(document.querySelector('script')).toBeNull();
  expect(screen.getByText(/Hello/)).toBeDefined();
});
