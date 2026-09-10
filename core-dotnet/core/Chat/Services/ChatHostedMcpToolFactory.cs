using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools() => [CreateGeoTool(), CreateNonAiWeatherTool()];

    // Agent Geo 👤's remote MCP tool — mcp-srv-func-app only.
    public IList<AITool> CreateGeoTools() => [CreateGeoTool()];

    // Agent NonAI Weather 👤's remote MCP tool — mcp-srv-app-service only.
    public IList<AITool> CreateNonAiWeatherTools() => [CreateNonAiWeatherTool()];

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
}
