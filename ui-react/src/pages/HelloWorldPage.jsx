import { useGetHelloQuery } from '../services/weatherApi';

function HelloWorldPage() {
  const { data: helloMessage, isError: isHelloError } = useGetHelloQuery();

  return (
    <main className="mx-auto w-full max-w-5xl flex-1 overflow-y-auto p-4">
      <section aria-labelledby="hello-world-heading">
        <h2 id="hello-world-heading" className="mb-2 text-xl font-semibold">
          Hello World
        </h2>
        <p className="text-base leading-normal text-gray-800">
          {isHelloError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
        </p>
      </section>
    </main>
  );
}

export default HelloWorldPage;
