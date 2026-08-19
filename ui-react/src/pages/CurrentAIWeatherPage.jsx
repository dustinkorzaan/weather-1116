import { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import CurrentAIWeatherV3Tab from '../components/currentAiWeather/CurrentAIWeatherV3Tab';
import CurrentAIWeatherV4Tab from '../components/currentAiWeather/CurrentAIWeatherV4Tab';
import CurrentAIWeatherV5Tab from '../components/currentAiWeather/CurrentAIWeatherV5Tab';

const TAB_CONFIG = [
  { id: 'v3', label: 'V3', description: 'In-process tool loop · Like Foundry Console V3' },
  { id: 'v4', label: 'V4', description: 'Remote MCP tools · Like Foundry Console V4' },
  { id: 'v5', label: 'V5', description: 'Hosted Foundry agent · Like Foundry Console V5' },
];

const TAB_COMPONENTS = {
  v3: CurrentAIWeatherV3Tab,
  v4: CurrentAIWeatherV4Tab,
  v5: CurrentAIWeatherV5Tab,
};

function CurrentAIWeatherPage() {
  const [activeTab, setActiveTab] = useState('v3');
  const activeConfig = TAB_CONFIG.find((tab) => tab.id === activeTab);

  return (
    <main className="mx-auto w-full max-w-5xl flex-1 overflow-y-auto p-4">
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          {TAB_CONFIG.map(({ id, label }) => (
            <TabsTrigger key={id} value={id}>
              {label}
            </TabsTrigger>
          ))}
        </TabsList>
        <p className="mt-2 text-sm text-muted-foreground">{activeConfig.description}</p>
        {TAB_CONFIG.map(({ id }) => {
          const TabComponent = TAB_COMPONENTS[id];
          return (
            <TabsContent key={id} value={id} className="mt-3">
              <TabComponent />
            </TabsContent>
          );
        })}
      </Tabs>
    </main>
  );
}

export default CurrentAIWeatherPage;
