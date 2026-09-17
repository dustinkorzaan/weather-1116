import { blazorBaseUrl, mcpSrvAppServiceBaseUrl, mcpSrvFuncAppBaseUrl, mvcBaseUrl } from '../../config/siteLinks';
import { resolveApiBaseUrl } from '../../services/apiBaseUrl';

// ACA scales every layer to zero when idle, and a cold start can take up to ~90s per
// layer. API, mcp-srv-app-service, and mcp-srv-func-app each expose a dedicated
// anonymous /Wake probe (mcp-srv-func-app: /wake) instead of /About -- /About does
// real work (API's fans out to worker + both MCP hosts via Task.WhenAll, the MCP
// hosts introspect their tool list) that a liveness ping doesn't need and that only
// slows down a container still booting on 0.25 vCPU/0.5Gi. MVC and Blazor have no
// API surface to probe, so a plain GET to their root is enough to wake them.
export const WAKE_TARGETS = [
  { key: 'api', label: 'API', url: `${resolveApiBaseUrl()}/Wake` },
  { key: 'mcpSrvAppService', label: 'MCP App Service', url: `${mcpSrvAppServiceBaseUrl}/Wake` },
  { key: 'mcpSrvFuncApp', label: 'MCP Func App', url: `${mcpSrvFuncAppBaseUrl}/wake` },
  { key: 'mvc', label: 'MVC', url: mvcBaseUrl },
  { key: 'blazor', label: 'Blazor', url: blazorBaseUrl },
];
