/**
 * Fetches the weather forecast from the Weather API's /weatherforecast endpoint
 * (proxied by Vite in development). Mirrors the data shown on the Blazor home view.
 */
export async function fetchForecast() {
  const response = await fetch('/weatherforecast');
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}
