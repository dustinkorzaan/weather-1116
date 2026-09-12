import { Component } from 'react';
import { ApplicationInsights, DistributedTracingModes } from '@microsoft/applicationinsights-web';
import { AppInsightsContext, ReactPlugin } from '@microsoft/applicationinsights-react-js';

export const reactPlugin = new ReactPlugin();

const connectionString = import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING;

// Scope CORS correlation to the API host only -- enabling it with no allow-list makes the
// SDK attempt to inject traceparent/Request-Id on every cross-origin fetch/XHR (Blazor/MVC
// warmup pings in App.jsx, Google Maps), not just api-dotnet.
function apiHost() {
  try {
    return new URL(import.meta.env.VITE_API_DOTNET_URL).hostname;
  } catch {
    return undefined;
  }
}

// Same Application Insights resource the .NET apps export OpenTelemetry to
// (infra/modules/monitoring.bicep) -- enableCorsCorrelation links a page's
// trace to the api-dotnet request it triggers via the W3C traceparent header.
export const appInsights = connectionString
  ? new ApplicationInsights({
      config: {
        connectionString,
        extensions: [reactPlugin],
        enableAutoRouteTracking: true,
        enableCorsCorrelation: true,
        correlationHeaderDomains: apiHost() ? [apiHost()] : [],
        enableUnhandledPromiseRejectionTracking: true,
        autoTrackPageVisitTime: true,
        distributedTracingMode: DistributedTracingModes.W3C,
      },
    })
  : null;

if (appInsights) {
  appInsights.loadAppInsights();
  // All apps share wx1116-prod-appinsights; without this the SPA's telemetry is
  // indistinguishable from the backends in Application Map.
  appInsights.addTelemetryInitializer((envelope) => {
    envelope.tags = envelope.tags || {};
    envelope.tags['ai.cloud.role'] = 'ui-react';
  });
}

function ErrorFallback() {
  return (
    <div className="flex min-h-screen items-center justify-center p-6 text-center">
      <div>
        <p className="text-lg font-semibold">Something went wrong.</p>
        <p className="text-sm text-muted-foreground">
          The error has been reported. Try reloading the page.
        </p>
      </div>
    </div>
  );
}

// A plain boundary, not applicationinsights-react-js's AppInsightsErrorBoundary --
// that component calls appInsights.trackException() unconditionally in
// componentDidCatch, which throws if the SDK was never initialized (no connection
// string). Crash handling must not depend on whether telemetry is configured.
class ErrorBoundary extends Component {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error, errorInfo) {
    appInsights?.trackException({ error, exception: error, properties: errorInfo });
  }

  render() {
    return this.state.hasError ? <ErrorFallback /> : this.props.children;
  }
}

/** Telemetry reporting no-ops when VITE_APPINSIGHTS_CONNECTION_STRING is unset (e.g. local dev), but crash handling is always active. */
export function TelemetryProvider({ children }) {
  return (
    <AppInsightsContext.Provider value={reactPlugin}>
      <ErrorBoundary>{children}</ErrorBoundary>
    </AppInsightsContext.Provider>
  );
}
