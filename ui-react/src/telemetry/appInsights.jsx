import { ApplicationInsights, DistributedTracingModes } from '@microsoft/applicationinsights-web';
import { AppInsightsContext, AppInsightsErrorBoundary, ReactPlugin } from '@microsoft/applicationinsights-react-js';

export const reactPlugin = new ReactPlugin();

const connectionString = import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING;

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
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true,
        enableUnhandledPromiseRejectionTracking: true,
        autoTrackPageVisitTime: true,
        distributedTracingMode: DistributedTracingModes.W3C,
      },
    })
  : null;

appInsights?.loadAppInsights();

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

/** No-ops (renders children as-is) when VITE_APPINSIGHTS_CONNECTION_STRING is unset, e.g. local dev. */
export function TelemetryProvider({ children }) {
  if (!appInsights) {
    return children;
  }

  return (
    <AppInsightsContext.Provider value={reactPlugin}>
      <AppInsightsErrorBoundary appInsights={reactPlugin} onError={ErrorFallback}>
        {children}
      </AppInsightsErrorBoundary>
    </AppInsightsContext.Provider>
  );
}
