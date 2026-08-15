using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools()
    {
        var mcpFunctionUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_URL.");
        var mcpFunctionKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_KEY.");

        var mcpAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_URL.");
        var mcpAppKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_KEY.");

        return
        [
            new HostedMcpServerTool(
                "McpSrvFuncApp",
                new Uri($"{mcpFunctionUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
                new Dictionary<string, object?> { ["x-functions-key"] = mcpFunctionKey })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
            new HostedMcpServerTool(
                "McpSrvAppService",
                new Uri($"{mcpAppUrl.TrimEnd('/')}/mcp"),
                new Dictionary<string, object?> { ["Authorization"] = $"Bearer {mcpAppKey}" })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
        ];
    }
}
