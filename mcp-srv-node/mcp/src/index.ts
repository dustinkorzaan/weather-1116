import { existsSync } from 'node:fs';
import { useAzureMonitor } from '@azure/monitor-opentelemetry';

if (existsSync('.env')) {
  process.loadEnvFile('.env');
}

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/container-app.bicep), mirroring the other MCP hosts. It's opt-in: local dev
// and test runs have no App Insights resource. A whitespace-only value counts as unset, mirroring
// .NET's IsNullOrWhiteSpace guard. Must run before the server module loads so Express and outgoing
// HTTP calls get instrumented.
if (process.env.APPLICATIONINSIGHTS_CONNECTION_STRING?.trim()) {
  useAzureMonitor();
}

const { main } = await import('./server.ts');
main();
