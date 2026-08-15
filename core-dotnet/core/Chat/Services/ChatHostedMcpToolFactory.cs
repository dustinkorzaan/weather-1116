using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools()
    {
        var mcpSrvFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_URL.");
        var mcpSrvFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_KEY.");

        var mcpSrvAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_URL.");
        var mcpSrvAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_KEY.");

        return
        [
            new HostedMcpServerTool(
                "McpSrvFuncApp",
                new Uri($"{mcpSrvFuncAppUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
                new Dictionary<string, object?> { ["x-functions-key"] = mcpSrvFuncAppKey })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
            new HostedMcpServerTool(
                "McpSrvAppService",
                new Uri($"{mcpSrvAppServiceUrl.TrimEnd('/')}/mcp"),
                new Dictionary<string, object?> { ["Authorization"] = $"Bearer {mcpSrvAppServiceKey}" })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
        ];
    }
}
