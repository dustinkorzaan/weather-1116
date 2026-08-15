using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatMcpToolFactory
{
    public (McpTool LatLong, McpTool Weather) CreateTools()
    {
        var mcpFunctionUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_URL.");
        var mcpFunctionKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_KEY.");

        var mcpAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_URL.");
        var mcpAppKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_KEY.");

        McpTool latLong = ResponseTool.CreateMcpTool(
            serverLabel: "McpSrvFuncApp",
            serverUri: new Uri($"{mcpFunctionUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
            headers: new Dictionary<string, string> { ["x-functions-key"] = mcpFunctionKey },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        McpTool weather = ResponseTool.CreateMcpTool(
            serverLabel: "McpSrvAppService",
            serverUri: new Uri($"{mcpAppUrl.TrimEnd('/')}/mcp"),
            headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {mcpAppKey}" },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        return (latLong, weather);
    }
}
