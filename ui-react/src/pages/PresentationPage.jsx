import ChatPanel from '../components/chat/ChatPanel';
import CurrentAIWeather from '../components/CurrentAIWeather';
import { useGetHelloQuery } from '../services/weatherApi';

function PresentationPage() {
  const { data: helloMessage, isError: isHelloError } = useGetHelloQuery();

  return (
    <main className="mx-auto w-full max-w-5xl flex-1 overflow-y-auto p-4">
      <p className="text-base font-medium">
        {isHelloError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
      </p>

      <CurrentAIWeather />

      <ChatPanel />
    </main>
  );
}

export default PresentationPage;
