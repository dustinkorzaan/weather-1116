import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test } from 'vitest';

test('Azure Static Web Apps rewrites unknown routes to index.html', () => {
  const configPath = join(
    dirname(fileURLToPath(import.meta.url)),
    '../public/staticwebapp.config.json'
  );
  const config = JSON.parse(readFileSync(configPath, 'utf8'));

  expect(config.navigationFallback.rewrite).toBe('/index.html');
  expect(config.navigationFallback.exclude).toContain('/assets/*');
});
