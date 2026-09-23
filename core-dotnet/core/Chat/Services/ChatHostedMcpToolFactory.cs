using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools() =>
        [CreateGeoTool(), CreateNonAiWeatherTool(), CreateNonAiWeatherPythonTool(), CreateNonAiWeatherNodeTool()];

    // Agent Geo 👤's remote MCP tool — mcp-srv-func-app only.
    public IList<AITool> CreateGeoTools() => [CreateGeoTool()];

    // Agent NonAI Weather 👤's remote MCP tools — mcp-srv-app-service (current) plus
    // mcp-srv-python and mcp-srv-node; each forecast/history tool is registered on exactly one of
    // those two at a time (both on mcp-srv-node today), so both hosts stay attached.
    public IList<AITool> CreateNonAiWeatherTools() =>
        [CreateNonAiWeatherTool(), CreateNonAiWeatherPythonTool(), CreateNonAiWeatherNodeTool()];

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

    private static HostedMcpServerTool CreateNonAiWeatherPythonTool()
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
