import './App.css';
import { useEffect, useState } from 'react';

function App() {
  const [helloMessage, setHelloMessage] = useState('Loading hello message...');

  useEffect(() => {
    let isMounted = true;

    fetch('/Home/Hello')
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Request failed: ${response.status}`);
        }

        return response.json();
      })
      .then((data) => {
        if (isMounted) {
          setHelloMessage(data.requestResponse ?? 'No hello response returned.');
        }
      })
      .catch(() => {
        if (isMounted) {
          setHelloMessage('Unable to load hello message from API.');
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <div className="app">
      <header className="top-bar">
        <div className="site-brand">
          <img src="/logo.svg" alt="Weather logo" className="site-logo" />
          <h1 className="title">Weather React</h1>
        </div>
      </header>

      <main className="home-content">
        <p className="hello-message">{helloMessage}</p>
      </main>
    </div>
  );
}

export default App;
