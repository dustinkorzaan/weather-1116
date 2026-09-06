function trimUrl(url) {
  return url?.replace(/\/$/, '') ?? '';
}

const apiBaseUrl = trimUrl(import.meta.env.VITE_API_DOTNET_URL) || 'http://localhost:8080';
const workerBaseUrl = trimUrl(import.meta.env.VITE_WORKER_DOTNET_URL) || 'http://localhost:8130';
export const blazorBaseUrl = trimUrl(import.meta.env.VITE_UI_BLAZOR_URL) || 'http://localhost:8090';
export const mvcBaseUrl = trimUrl(import.meta.env.VITE_MVC_DOTNET_URL) || 'http://localhost:8100';

export const siteLinks = [
  {
    label: 'UI Blazor',
    href: blazorBaseUrl,
  },
  {
    label: 'MVC',
    href: mvcBaseUrl,
  },
  {
    label: 'API About',
    href: `${apiBaseUrl}/About`,
  },
  {
    label: 'Worker Hangfire',
    href: `${workerBaseUrl}/hangfire`,
  },
];
