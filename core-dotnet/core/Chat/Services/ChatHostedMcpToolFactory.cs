using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

public sealed class ChatHostedMcpToolFactory
{
    public IList<AITool> CreateTools()
    {
        var mcpFunctionUrl = Environment.GetEnvironmentVariable("MCP_FUNCTION_URL")
            ?? throw new InvalidOperationException("Missing MCP_FUNCTION_URL.");
        var mcpFunctionKey = Environment.GetEnvironmentVariable("MCP_FUNCTION_KEY")
            ?? throw new InvalidOperationException("Missing MCP_FUNCTION_KEY.");

        var mcpAppUrl = Environment.GetEnvironmentVariable("MCP_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_APP_URL.");
        var mcpAppKey = Environment.GetEnvironmentVariable("MCP_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_APP_KEY.");

        return
        [
            new HostedMcpServerTool(
                "MyMCPFunction",
                new Uri($"{mcpFunctionUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
                new Dictionary<string, object?> { ["x-functions-key"] = mcpFunctionKey })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
            new HostedMcpServerTool(
                "MyMCPApp",
                new Uri($"{mcpAppUrl.TrimEnd('/')}/mcp"),
                new Dictionary<string, object?> { ["Authorization"] = $"Bearer {mcpAppKey}" })
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
        ];
    }
}
