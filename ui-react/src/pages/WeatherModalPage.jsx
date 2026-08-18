import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import CurrentAIWeatherModalTab from '../components/weatherModal/CurrentAIWeatherModalTab';
import ComingSoonTab from '../components/weatherModal/ComingSoonTab';
import { WEATHER_MODAL_TAB_CONFIG } from '../components/weatherModal/weatherModalTabs';
import { formatLocationWithLatLong } from '../utils/currentAiWeatherLocation';
import {
  weatherModalParamsFromSearchParams,
  weatherModalPath,
} from '../utils/weatherModalLocation';

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
            {WEATHER_MODAL_TAB_CONFIG.map(({ value, label, icon }) => (
              <TabsContent key={value} value={value}>
                {value === 'current' ? (
                  <CurrentAIWeatherModalTab name={name} lat={lat} lng={lng} />
                ) : (
                  <ComingSoonTab label={label} icon={icon} />
                )}
              </TabsContent>
            ))}
          </div>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}

export default WeatherModalPage;
