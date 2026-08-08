export function resolveApiBaseUrl() {
  const configuredBaseUrl = import.meta.env.VITE_API_DOTNET_URL?.replace(/\/$/, '');
  if (configuredBaseUrl) {
    return configuredBaseUrl;
  }

  if (typeof window !== 'undefined' && window.location?.origin) {
    return window.location.origin;
  }

  return '';
}
