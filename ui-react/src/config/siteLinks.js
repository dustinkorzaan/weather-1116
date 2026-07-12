function trimUrl(url) {
  return url?.replace(/\/$/, '') ?? '';
}

const apiBaseUrl = trimUrl(import.meta.env.VITE_API_DOTNET_URL) || 'http://localhost:8080';

export const siteLinks = [
  {
    label: 'UI React',
    href: import.meta.env.VITE_UI_REACT_URL || 'http://localhost:3000',
  },
  {
    label: 'UI Blazor',
    href: import.meta.env.VITE_UI_BLAZOR_URL || 'http://localhost:8090',
  },
  {
    label: 'MVC',
    href: import.meta.env.VITE_MVC_DOTNET_URL || 'http://localhost:8100',
  },
  {
    label: 'API About',
    href: `${apiBaseUrl}/About`,
  },
];
