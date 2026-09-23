function trimUrl(url) {
  return url?.replace(/\/$/, '') ?? '';
}

export const apiBaseUrl = trimUrl(import.meta.env.VITE_API_DOTNET_URL) || 'http://localhost:8080';
export const workerBaseUrl = trimUrl(import.meta.env.VITE_WORKER_DOTNET_URL) || 'http://localhost:8130';
export const blazorBaseUrl = trimUrl(import.meta.env.VITE_UI_BLAZOR_URL) || 'http://localhost:8090';
export const mvcBaseUrl = trimUrl(import.meta.env.VITE_MVC_DOTNET_URL) || 'http://localhost:8100';
export const mcpSrvAppServiceBaseUrl =
  trimUrl(import.meta.env.VITE_MCP_SRV_APP_SERVICE_URL) || 'http://localhost:8110';
export const mcpSrvFuncAppBaseUrl =
  trimUrl(import.meta.env.VITE_MCP_SRV_FUNC_APP_URL) || 'http://localhost:8120';
export const mcpSrvPythonBaseUrl =
  trimUrl(import.meta.env.VITE_MCP_SRV_PYTHON_URL) || 'http://localhost:8140';
export const mcpSrvNodeBaseUrl =
  trimUrl(import.meta.env.VITE_MCP_SRV_NODE_URL) || 'http://localhost:8150';
