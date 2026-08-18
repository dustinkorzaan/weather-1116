import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { buildUiReactRoot } from './about';
import { resolveApiBaseUrl } from './apiBaseUrl';

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
    searchLocation: builder.query({
      query: (location) => `/Geo?location=${encodeURIComponent(location || '')}`,
    }),
    getLocation: builder.query({
      query: ({ latitude, longitude }) =>
        `/Geo/GetLocation?latitude=${encodeURIComponent(latitude)}&longitude=${encodeURIComponent(longitude)}`,
    }),
  }),
});

export const {
  useGetHelloQuery,
  useLazyGetAboutQuery,
  useGetCurrentAIWeatherQuery,
  useLazyGetCurrentAIWeatherQuery,
  useLazySearchLocationQuery,
  useLazyGetLocationQuery,
} = weatherApi;
