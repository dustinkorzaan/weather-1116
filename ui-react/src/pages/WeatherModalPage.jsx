import { CalendarClock, CalendarDays, Clock, History, Sparkles, Timer } from 'lucide-react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import CurrentAIWeatherModalTab from '../components/weatherModal/CurrentAIWeatherModalTab';
import DailyForecastTab from '../components/weatherModal/DailyForecastTab';
import HourlyForecastTab from '../components/weatherModal/HourlyForecastTab';
import Every15ForecastTab from '../components/weatherModal/Every15ForecastTab';
import DailyHistoryTab from '../components/weatherModal/DailyHistoryTab';
import HourlyHistoryTab from '../components/weatherModal/HourlyHistoryTab';
import { formatLocationWithLatLong } from '../utils/currentAiWeatherLocation';
import {
  weatherModalParamsFromSearchParams,
  weatherModalPath,
  WEATHER_MODAL_TABS,
} from '../utils/weatherModalLocation';

const TAB_COMPONENTS = {
  current: CurrentAIWeatherModalTab,
  'daily-forecast': DailyForecastTab,
  'hourly-forecast': HourlyForecastTab,
  'every-15-forecast': Every15ForecastTab,
  'daily-history': DailyHistoryTab,
  'hourly-history': HourlyHistoryTab,
};

/** Icon + label for each weather modal tab, in display order. */
const WEATHER_MODAL_TAB_CONFIG = [
  { value: WEATHER_MODAL_TABS[0], label: 'Current AI Weather', icon: Sparkles },
  { value: WEATHER_MODAL_TABS[1], label: 'Daily Forecast', icon: CalendarDays },
  { value: WEATHER_MODAL_TABS[2], label: 'Hourly Forecast', icon: Clock },
  { value: WEATHER_MODAL_TABS[3], label: 'Every 15 Forecast', icon: Timer },
  { value: WEATHER_MODAL_TABS[4], label: 'Daily History', icon: CalendarClock },
  { value: WEATHER_MODAL_TABS[5], label: 'Hourly History', icon: History },
];

function WeatherModalPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { name, lat, lng, tab } = weatherModalParamsFromSearchParams(searchParams);
  const title = formatLocationWithLatLong(name, lat, lng) || name || 'Location';

  const handleTabChange = (nextTab) => {
    navigate(weatherModalPath({ name, lat, lng, tab: nextTab }), { replace: true });
  };

  const handleOpenChange = (open) => {
    if (!open) {
      navigate('/');
    }
  };

  return (
    <Dialog open onOpenChange={handleOpenChange}>
      <DialogContent className="top-4 right-4 bottom-4 left-4 flex h-auto w-auto max-w-none translate-x-0 translate-y-0 flex-col gap-3 overflow-hidden sm:top-8 sm:right-8 sm:bottom-8 sm:left-8 sm:max-w-none">
        <DialogHeader>
          <DialogTitle className="text-lg">{title}</DialogTitle>
        </DialogHeader>
        <Tabs value={tab} onValueChange={handleTabChange} className="min-h-0 flex-1">
          <TabsList className="w-full">
            {WEATHER_MODAL_TAB_CONFIG.map(({ value, label, icon: Icon }) => (
              <TabsTrigger key={value} value={value} className="flex-1" title={label} aria-label={label}>
                <Icon aria-hidden="true" />
                <span className="hidden sm:inline">{label}</span>
              </TabsTrigger>
            ))}
          </TabsList>
          <div className="min-h-0 flex-1 overflow-y-auto pt-3">
            {WEATHER_MODAL_TAB_CONFIG.map(({ value }) => {
              const TabComponent = TAB_COMPONENTS[value];
              return (
                <TabsContent key={value} value={value}>
                  <TabComponent name={name} lat={lat} lng={lng} />
                </TabsContent>
              );
            })}
          </div>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}

export default WeatherModalPage;
