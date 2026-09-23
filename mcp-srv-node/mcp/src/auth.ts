import { createHash, timingSafeEqual } from 'node:crypto';
import type { RequestHandler } from 'express';

const BEARER_PREFIX = 'Bearer ';

// Hashing both sides gives timingSafeEqual equal-length inputs regardless of what was presented.
function tokensMatch(presented: string, expected: string): boolean {
  const presentedHash = createHash('sha256').update(presented).digest();
  const expectedHash = createHash('sha256').update(expected).digest();
  return timingSafeEqual(presentedHash, expectedHash);
}

/** Requires a valid `Authorization: Bearer <token>` header on every request under `protectedPathPrefix`. */
export function bearerTokenAuth(token: string, protectedPathPrefix = '/mcp'): RequestHandler {
  return (req, res, next) => {
    if (!req.path.startsWith(protectedPathPrefix)) {
      next();
      return;
    }

    const header = req.get('Authorization') ?? '';
    if (!token || !header.startsWith(BEARER_PREFIX) || !tokensMatch(header.slice(BEARER_PREFIX.length).trim(), token)) {
      res.status(401).type('text/plain').send('Unauthorized');
      return;
    }

    next();
  };
}
