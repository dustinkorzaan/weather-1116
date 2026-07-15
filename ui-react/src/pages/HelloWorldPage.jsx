import { useGetHelloQuery } from '../services/weatherApi';

function HelloWorldPage() {
  const { data: helloMessage, isError } = useGetHelloQuery();

  return (
    <main className="home-content">
      <h2>Hello World</h2>
      <p className="hello-message">
        {isError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
      </p>
    </main>
  );
}

export default HelloWorldPage;
