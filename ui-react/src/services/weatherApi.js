import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { buildUiReactRoot } from './about';
import { resolveApiBaseUrl } from './apiBaseUrl';

const apiBaseUrl = resolveApiBaseUrl();

export const weatherApi = createApi({
  reducerPath: 'weatherApi',
  baseQuery: fetchBaseQuery({ baseUrl: apiBaseUrl }),
  tagTypes: ['User'],
  endpoints: (builder) => ({
    getHello: builder.query({
      query: () => '/Home/Hello',
      transformResponse: (response) => response?.requestResponse ?? 'No hello response returned.',
    }),
    getAbout: builder.query({
      query: () => '/About',
      transformResponse: (apiRoot) => buildUiReactRoot(apiRoot),
    }),
    getCurrentAIWeatherV3: builder.query({
      query: (location) =>
        `/AIWeather/CurrentV3?location=${encodeURIComponent(location || 'Nashville, TN')}`,
    }),
    getCurrentAIWeatherV4: builder.query({
      query: (location) =>
        `/AIWeather/CurrentV4?location=${encodeURIComponent(location || 'Nashville, TN')}`,
    }),
    getCurrentAIWeatherV5: builder.query({
      query: (location) =>
        `/AIWeather/CurrentV5?location=${encodeURIComponent(location || 'Nashville, TN')}`,
    }),
    searchLocation: builder.query({
      query: (location) => `/Geo?location=${encodeURIComponent(location || '')}`,
    }),
    getLocation: builder.query({
      query: ({ latitude, longitude }) =>
        `/Geo/GetLocation?latitude=${encodeURIComponent(latitude)}&longitude=${encodeURIComponent(longitude)}`,
    }),
    getForecast: builder.query({
      query: ({ latitude, longitude, resolution }) =>
        `/Forecast?latitude=${encodeURIComponent(latitude)}&longitude=${encodeURIComponent(longitude)}&resolution=${encodeURIComponent(resolution)}`,
    }),
    getHistory: builder.query({
      query: ({ latitude, longitude, resolution }) =>
        `/History?latitude=${encodeURIComponent(latitude)}&longitude=${encodeURIComponent(longitude)}&resolution=${encodeURIComponent(resolution)}`,
    }),
    getUser: builder.query({
      query: () => '/User',
      providesTags: ['User'],
    }),
    addUserCity: builder.mutation({
      query: (pin) => ({
        url: '/User/AddCity',
        method: 'POST',
        body: pin,
      }),
      invalidatesTags: ['User'],
    }),
    deleteUserCity: builder.mutation({
      query: (userCityId) => ({
        url: `/User/DeleteCity?userCityId=${encodeURIComponent(userCityId)}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['User'],
    }),
  }),
});

export const {
  useGetHelloQuery,
  useLazyGetAboutQuery,
  useGetCurrentAIWeatherV3Query,
  useLazyGetCurrentAIWeatherV3Query,
  useGetCurrentAIWeatherV4Query,
  useLazyGetCurrentAIWeatherV4Query,
  useGetCurrentAIWeatherV5Query,
  useLazyGetCurrentAIWeatherV5Query,
  useLazySearchLocationQuery,
  useLazyGetLocationQuery,
  useGetForecastQuery,
  useGetHistoryQuery,
  useGetUserQuery,
  useLazyGetUserQuery,
  useAddUserCityMutation,
  useDeleteUserCityMutation,
} = weatherApi;
