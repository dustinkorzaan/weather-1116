import React from 'react';
import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import './index.css';
import App from './App';
import { BackendWakeGate } from './components/wake/BackendWakeGate';
import { store } from './app/store';
import { TelemetryProvider } from './telemetry/appInsights';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <TelemetryProvider>
      <Provider store={store}>
        <BrowserRouter>
          <BackendWakeGate>
            <App />
          </BackendWakeGate>
        </BrowserRouter>
      </Provider>
    </TelemetryProvider>
  </React.StrictMode>
);
