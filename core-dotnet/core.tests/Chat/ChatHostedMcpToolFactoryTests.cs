using Core.Chat.Services;
using Microsoft.Extensions.AI;

namespace Core.Tests.Chat;

public class ChatHostedMcpToolFactoryTests
{
    [Fact]
    public void CreateTools_PutsAuthOnHeadersNotAdditionalProperties()
    {
        RunWithMcpEnvironment(
            funcAppUrl: "https://func.example.com/",
            funcAppKey: "func-key",
            appServiceUrl: "https://app.example.com/",
            appServiceKey: "app-key",
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                Assert.Equal(2, tools.Count);

                var funcApp = Assert.Single(tools, tool => tool.ServerName == "McpSrvFuncApp");
                Assert.Equal("https://func.example.com/runtime/webhooks/mcp", funcApp.ServerAddress);
                Assert.Equal(HostedMcpServerToolApprovalMode.NeverRequire, funcApp.ApprovalMode);
                Assert.NotNull(funcApp.Headers);
                Assert.Equal("func-key", funcApp.Headers["x-functions-key"]);
                Assert.DoesNotContain("x-functions-key", funcApp.AdditionalProperties.Keys);

                var appService = Assert.Single(tools, tool => tool.ServerName == "McpSrvAppService");
                Assert.Equal("https://app.example.com/mcp", appService.ServerAddress);
                Assert.Equal(HostedMcpServerToolApprovalMode.NeverRequire, appService.ApprovalMode);
                Assert.NotNull(appService.Headers);
                Assert.Equal("Bearer app-key", appService.Headers["Authorization"]);
                Assert.DoesNotContain("Authorization", appService.AdditionalProperties.Keys);
            });
    }

    [Fact]
    public void CreateTools_ThrowsWhenMcpEnvironmentIsMissing()
    {
        RunWithMcpEnvironment(null, null, null, null, () =>
        {
            var factory = new ChatHostedMcpToolFactory();
            var ex = Assert.Throws<InvalidOperationException>(factory.CreateTools);
            Assert.StartsWith("Missing MCP_SRV_", ex.Message, StringComparison.Ordinal);
        });
    }

    private static void RunWithMcpEnvironment(
        string? funcAppUrl,
        string? funcAppKey,
        string? appServiceUrl,
        string? appServiceKey,
        Action action)
    {
        var previousFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL");
        var previousFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY");
        var previousAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL");
        var previousAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY");
        try
        {
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_URL", funcAppUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY", funcAppKey);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL", appServiceUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY", appServiceKey);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_URL", previousFuncAppUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY", previousFuncAppKey);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL", previousAppServiceUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY", previousAppServiceKey);
        }
    }
}
