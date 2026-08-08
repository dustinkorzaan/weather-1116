using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatMcpToolFactory
{
    public (McpTool LatLong, McpTool Weather) CreateTools()
    {
        var mcpFunctionUrl = Environment.GetEnvironmentVariable("MCP_FUNCTION_URL")
            ?? throw new InvalidOperationException("Missing MCP_FUNCTION_URL.");
        var mcpFunctionKey = Environment.GetEnvironmentVariable("MCP_FUNCTION_KEY")
            ?? throw new InvalidOperationException("Missing MCP_FUNCTION_KEY.");

        var mcpAppUrl = Environment.GetEnvironmentVariable("MCP_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_APP_URL.");
        var mcpAppKey = Environment.GetEnvironmentVariable("MCP_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_APP_KEY.");

        McpTool latLong = ResponseTool.CreateMcpTool(
            serverLabel: "MyMCPFunction",
            serverUri: new Uri($"{mcpFunctionUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
            headers: new Dictionary<string, string> { ["x-functions-key"] = mcpFunctionKey },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        McpTool weather = ResponseTool.CreateMcpTool(
            serverLabel: "MyMCPApp",
            serverUri: new Uri($"{mcpAppUrl.TrimEnd('/')}/mcp"),
            headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {mcpAppKey}" },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        return (latLong, weather);
    }
}
