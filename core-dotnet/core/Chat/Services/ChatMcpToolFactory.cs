using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatMcpToolFactory
{
    public (McpTool LatLong, McpTool Weather) CreateTools()
    {
        var mcpSrvFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_URL.");
        var mcpSrvFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_FUNC_APP_KEY.");

        var mcpSrvAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_URL.");
        var mcpSrvAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY")
            ?? throw new InvalidOperationException("Missing MCP_SRV_APP_SERVICE_KEY.");

        McpTool latLong = ResponseTool.CreateMcpTool(
            serverLabel: "McpSrvFuncApp",
            serverUri: new Uri($"{mcpSrvFuncAppUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
            headers: new Dictionary<string, string> { ["x-functions-key"] = mcpSrvFuncAppKey },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        McpTool weather = ResponseTool.CreateMcpTool(
            serverLabel: "McpSrvAppService",
            serverUri: new Uri($"{mcpSrvAppServiceUrl.TrimEnd('/')}/mcp"),
            headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {mcpSrvAppServiceKey}" },
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        return (latLong, weather);
    }
}
