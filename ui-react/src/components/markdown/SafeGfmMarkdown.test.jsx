import { expect, test } from 'vitest';
import { render, screen } from '@testing-library/react';
import SafeGfmMarkdown from './SafeGfmMarkdown';

test('renders GFM tables and emphasis after the markdown is complete', () => {
  render(
    <SafeGfmMarkdown>
      {`**Warmest** today:

| City | Temp |
| --- | --- |
| Nashville | 72 |
| Atlanta | 80 |
`}
    </SafeGfmMarkdown>
  );

  expect(screen.getByRole('table')).toBeDefined();
  expect(screen.getByText('Nashville')).toBeDefined();
  expect(screen.getByText('Atlanta')).toBeDefined();
  expect(screen.getByText('Warmest').tagName).toBe('STRONG');
});

test('does not render raw HTML from the model', () => {
  render(
    <SafeGfmMarkdown>{'Hello <script>alert(1)</script> world'}</SafeGfmMarkdown>
  );

  expect(document.querySelector('script')).toBeNull();
  expect(screen.getByText(/Hello/)).toBeDefined();
});

test('does not keep javascript links or turn youtube URLs into embeds', () => {
  render(
    <SafeGfmMarkdown>
      {'[x](javascript:alert(1)) [watch](https://www.youtube.com/watch?v=dQw4w9WgXcQ)'}
    </SafeGfmMarkdown>
  );

  expect(document.querySelector('a[href^="javascript:"]')).toBeNull();
  expect(document.querySelector('iframe')).toBeNull();
  expect(screen.getByRole('link', { name: 'watch' }).getAttribute('href')).toBe(
    'https://www.youtube.com/watch?v=dQw4w9WgXcQ'
  );
});
