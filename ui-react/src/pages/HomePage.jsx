import CurrentAIWeather from '../components/CurrentAIWeather';
import WeatherMap from '../components/WeatherMap';
import { useGetHelloQuery } from '../services/weatherApi';

function HomePage() {
  const { data: helloMessage, isError: isHelloError } = useGetHelloQuery();

  return (
    <main className="home-content">
      <p className="hello-message">
        {isHelloError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
      </p>

      <CurrentAIWeather />

      <WeatherMap />
    </main>
  );
}

export default HomePage;
