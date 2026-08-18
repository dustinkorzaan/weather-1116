import { CalendarClock, CalendarDays, Clock, History, Sparkles, Timer } from 'lucide-react';
import { WEATHER_MODAL_TABS } from '../../utils/weatherModalLocation';

/** Icon + label for each weather modal tab, in display order. */
export const WEATHER_MODAL_TAB_CONFIG = [
  { value: WEATHER_MODAL_TABS[0], label: 'Current AI Weather', icon: Sparkles },
  { value: WEATHER_MODAL_TABS[1], label: 'Daily Forecast', icon: CalendarDays },
  { value: WEATHER_MODAL_TABS[2], label: 'Hourly Forecast', icon: Clock },
  { value: WEATHER_MODAL_TABS[3], label: 'Every 15 Forecast', icon: Timer },
  { value: WEATHER_MODAL_TABS[4], label: 'Daily History', icon: CalendarClock },
  { value: WEATHER_MODAL_TABS[5], label: 'Hourly History', icon: History },
];
