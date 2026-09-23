using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools() =>
        [CreateGeoTool(), CreateGeoPythonTool(), CreateNonAiWeatherTool(), CreateNonAiWeatherNodeTool()];

    // Agent Geo 👤's remote MCP tools — mcp-srv-func-app (GetLatLong/GetLocation) and
    // mcp-srv-python (GetCities).
    public IList<AITool> CreateGeoTools() => [CreateGeoTool(), CreateGeoPythonTool()];

    // Agent NonAI Weather 👤's remote MCP tools — mcp-srv-app-service (current) and
    // mcp-srv-node (forecast/history).
    public IList<AITool> CreateNonAiWeatherTools() => [CreateNonAiWeatherTool(), CreateNonAiWeatherNodeTool()];

    private static HostedMcpServerTool CreateGeoTool()
    {
        var mcpSrvFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_URL.");
        var mcpSrvFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_KEY.");

        return new HostedMcpServerTool(
            "McpSrvFuncApp",
            new Uri($"{mcpSrvFuncAppUrl.TrimEnd('/')}/runtime/webhooks/mcp"))
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-functions-key"] = mcpSrvFuncAppKey,
            },
        };
    }

    private static HostedMcpServerTool CreateNonAiWeatherTool()
    {
        var mcpSrvAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_URL.");
        var mcpSrvAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_KEY.");

        return new HostedMcpServerTool(
            "McpSrvAppService",
            new Uri($"{mcpSrvAppServiceUrl.TrimEnd('/')}/mcp"))
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = $"Bearer {mcpSrvAppServiceKey}",
            },
        };
    }

    private static HostedMcpServerTool CreateGeoPythonTool()
    {
        var mcpSrvPythonUrl = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_PYTHON_URL.");
        var mcpSrvPythonKey = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_PYTHON_KEY.");

        return new HostedMcpServerTool(
            "McpSrvPython",
            new Uri($"{mcpSrvPythonUrl.TrimEnd('/')}/mcp"))
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = $"Bearer {mcpSrvPythonKey}",
            },
        };
    }

    private static HostedMcpServerTool CreateNonAiWeatherNodeTool()
    {
        var mcpSrvNodeUrl = Environment.GetEnvironmentVariable("MCP_SRV_NODE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_NODE_URL.");
        var mcpSrvNodeKey = Environment.GetEnvironmentVariable("MCP_SRV_NODE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_NODE_KEY.");

        return new HostedMcpServerTool(
            "McpSrvNode",
            new Uri($"{mcpSrvNodeUrl.TrimEnd('/')}/mcp"))
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = $"Bearer {mcpSrvNodeKey}",
            },
        };
    }
}
