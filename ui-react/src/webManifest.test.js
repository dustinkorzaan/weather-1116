import { existsSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test } from 'vitest';

const publicDir = join(dirname(fileURLToPath(import.meta.url)), '../public');

test('ships Weather React web app manifest, not leftover CRA manifest.json', () => {
  expect(existsSync(join(publicDir, 'manifest.json'))).toBe(false);

  const manifest = JSON.parse(
    readFileSync(join(publicDir, 'manifest.webmanifest'), 'utf8')
  );

  expect(manifest.name).toBe('Weather React');
  expect(manifest.short_name).toBe('Weather React');
});
