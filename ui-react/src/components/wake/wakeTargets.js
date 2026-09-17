import {
  blazorBaseUrl,
  mcpSrvAppServiceBaseUrl,
  mcpSrvFuncAppBaseUrl,
  mvcBaseUrl,
  workerBaseUrl,
} from '../../config/siteLinks';
import { resolveApiBaseUrl } from '../../services/apiBaseUrl';

// Every layer here scales to zero when idle. API, worker, mcp-srv-app-service, and
// mcp-srv-func-app each expose an anonymous /Wake probe (mcp-srv-func-app: /wake)
// instead of /About -- /About does real work a liveness ping doesn't need (API's
// fans out to the other three, worker's and the MCP hosts' introspect their own
// health). MVC and Blazor have no API surface to probe, so a plain GET wakes them.
export const WAKE_TARGETS = [
  { key: 'api', label: 'API', url: `${resolveApiBaseUrl()}/Wake` },
  { key: 'worker', label: 'Worker', url: `${workerBaseUrl}/Wake` },
  { key: 'mcpSrvAppService', label: 'MCP App Service', url: `${mcpSrvAppServiceBaseUrl}/Wake` },
  { key: 'mcpSrvFuncApp', label: 'MCP Func App', url: `${mcpSrvFuncAppBaseUrl}/wake` },
  { key: 'mvc', label: 'MVC', url: mvcBaseUrl },
  { key: 'blazor', label: 'Blazor', url: blazorBaseUrl },
];
