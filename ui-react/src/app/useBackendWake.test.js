import { afterEach, expect, test, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useBackendWake } from './useBackendWake';
import { resolveApiBaseUrl } from '../services/apiBaseUrl';
import {
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../config/siteLinks';

// Worker and MCP App Service also ping their own /About, so matching on the
// path alone would stall them too -- match the full api URL instead.
const API_ABOUT_URL = `${resolveApiBaseUrl()}/About`;

afterEach(() => {
  vi.restoreAllMocks();
  vi.useRealTimers();
});

test('is not warm until the api, mvc, blazor, worker, and mcp host pings all resolve', async () => {
  let resolveAbout;
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url === API_ABOUT_URL) {
      return new Promise((resolve) => {
        resolveAbout = resolve;
      });
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  await waitFor(() => {
    expect(result.current.mvc).toBe(true);
    expect(result.current.blazor).toBe(true);
    expect(result.current.worker).toBe(true);
    expect(result.current.mcpSrvAppService).toBe(true);
    expect(result.current.mcpSrvFuncApp).toBe(true);
  });
  expect(result.current.api).toBe(false);
  expect(result.current.isWarm).toBe(false);

  resolveAbout(new Response(null, { status: 200 }));

  await waitFor(() => {
    expect(result.current.isWarm).toBe(true);
  });
});

test('pings all six targets at their expected URLs', () => {
  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockResolvedValue(new Response(null, { status: 200 }));

  renderHook(() => useBackendWake());

  const calledUrls = fetchMock.mock.calls.map(([input]) => String(input));
  expect(calledUrls).toEqual(
    expect.arrayContaining([
      API_ABOUT_URL,
      mvcBaseUrl,
      blazorBaseUrl,
      `${workerBaseUrl}/About`,
      `${mcpSrvAppServiceBaseUrl}/About`,
      `${mcpSrvFuncAppBaseUrl}/about`,
    ])
  );
});

test('retries a failed ping every ~30s until it succeeds', async () => {
  vi.useFakeTimers();
  let mvcCallCount = 0;
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url.includes('localhost:8100')) {
      mvcCallCount += 1;
      if (mvcCallCount === 1) {
        return Promise.reject(new Error('connection refused'));
      }
      return Promise.resolve(new Response(null, { status: 200 }));
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  await act(async () => {
    await vi.advanceTimersByTimeAsync(0);
  });
  expect(result.current.mvc).toBe(false);

  await act(async () => {
    await vi.advanceTimersByTimeAsync(30000);
  });

  expect(mvcCallCount).toBeGreaterThan(1);
  expect(result.current.mvc).toBe(true);
});

test('a slow first attempt still wins instead of being cancelled by later retries', async () => {
  vi.useFakeTimers();
  const apiResolvers = [];
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url === API_ABOUT_URL) {
      return new Promise((resolve) => {
        apiResolvers.push(resolve);
      });
    }
    return Promise.resolve(new Response(null, { status: 200 }));
  });

  const { result } = renderHook(() => useBackendWake());

  // Let a couple of retry intervals pass -- more requests fire, but none
  // has resolved yet, so none can have won.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(60000);
  });
  expect(apiResolvers.length).toBeGreaterThan(1);
  expect(result.current.api).toBe(false);

  // The very first (still-pending) attempt finally comes back -- it should
  // still count as the win, even though later retries were also in flight.
  await act(async () => {
    apiResolvers[0](new Response(null, { status: 200 }));
    await vi.advanceTimersByTimeAsync(0);
  });

  expect(result.current.api).toBe(true);
});
