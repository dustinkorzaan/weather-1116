/**
 * Fetches the weather forecast from the Weather API's /weatherforecast endpoint
 * (proxied by Vite in development). Mirrors the data shown on the Blazor home view.
 */
export async function fetchForecast() {
  const apiBaseUrl = import.meta.env.VITE_WEATHER1116_API_URL?.replace(/\/$/, '');
  const endpoint = apiBaseUrl ? `${apiBaseUrl}/weatherforecast` : '/weatherforecast';
  const response = await fetch(endpoint);
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}
