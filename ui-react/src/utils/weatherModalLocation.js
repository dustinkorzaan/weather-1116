export const WEATHER_MODAL_PATH = '/weather';

export const WEATHER_MODAL_TABS = [
  'current',
  'daily-forecast',
  'hourly-forecast',
  'every-15-forecast',
  'daily-history',
  'hourly-history',
];

export const DEFAULT_WEATHER_MODAL_TAB = WEATHER_MODAL_TABS[0];

const NAME_QUERY_PARAM = 'name';
const LAT_QUERY_PARAM = 'lat';
const LNG_QUERY_PARAM = 'lng';
const TAB_QUERY_PARAM = 'tab';

/** Builds "/weather?name=..&lat=..&lng=..&tab=..". */
export function weatherModalPath({ name, lat, lng, tab }) {
  const params = new URLSearchParams();
  if (name) {
    params.set(NAME_QUERY_PARAM, name);
  }
  if (Number.isFinite(Number(lat))) {
    params.set(LAT_QUERY_PARAM, lat);
  }
  if (Number.isFinite(Number(lng))) {
    params.set(LNG_QUERY_PARAM, lng);
  }
  params.set(TAB_QUERY_PARAM, WEATHER_MODAL_TABS.includes(tab) ? tab : DEFAULT_WEATHER_MODAL_TAB);

  return `${WEATHER_MODAL_PATH}?${params.toString()}`;
}

/** Reads { name, lat, lng, tab } back out of useSearchParams(). */
export function weatherModalParamsFromSearchParams(searchParams) {
  const name = (searchParams.get(NAME_QUERY_PARAM) ?? '').trim();
  const lat = Number(searchParams.get(LAT_QUERY_PARAM));
  const lng = Number(searchParams.get(LNG_QUERY_PARAM));
  const rawTab = searchParams.get(TAB_QUERY_PARAM);
  const tab = WEATHER_MODAL_TABS.includes(rawTab) ? rawTab : DEFAULT_WEATHER_MODAL_TAB;

  return {
    name,
    lat: Number.isFinite(lat) ? lat : null,
    lng: Number.isFinite(lng) ? lng : null,
    tab,
  };
}
