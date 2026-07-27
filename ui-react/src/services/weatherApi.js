import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { buildUiReactRoot } from './about';

function resolveApiBaseUrl() {
  const configuredBaseUrl = import.meta.env.VITE_API_DOTNET_URL?.replace(/\/$/, '');
  if (configuredBaseUrl) {
    return configuredBaseUrl;
  }

  if (typeof window !== 'undefined' && window.location?.origin) {
    return window.location.origin;
  }

  return '';
}

const apiBaseUrl = resolveApiBaseUrl();

export const weatherApi = createApi({
  reducerPath: 'weatherApi',
  baseQuery: fetchBaseQuery({ baseUrl: apiBaseUrl }),
  endpoints: (builder) => ({
    getHello: builder.query({
      query: () => '/Home/Hello',
      transformResponse: (response) => response?.requestResponse ?? 'No hello response returned.',
    }),
    getAbout: builder.query({
      query: () => '/About',
      transformResponse: (apiRoot) => buildUiReactRoot(apiRoot),
    }),
    getCurrentAIWeather: builder.query({
      query: (location) =>
        `/AIWeather/Current?location=${encodeURIComponent(location || 'Nashville, TN')}`,
    }),
  }),
});

export const {
  useGetHelloQuery,
  useLazyGetAboutQuery,
  useLazyGetCurrentAIWeatherQuery,
} = weatherApi;
