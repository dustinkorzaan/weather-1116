using Core.Chat.Services;
using Microsoft.Extensions.AI;

namespace Core.Tests.Chat;

[Collection(McpEnvironmentCollection.Name)]
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
            pythonUrl: "https://python.example.com/",
            pythonKey: "python-key",
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                Assert.Equal(3, tools.Count);

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

                var python = Assert.Single(tools, tool => tool.ServerName == "McpSrvPython");
                Assert.Equal("https://python.example.com/mcp", python.ServerAddress);
                Assert.Equal(HostedMcpServerToolApprovalMode.NeverRequire, python.ApprovalMode);
                Assert.NotNull(python.Headers);
                Assert.Equal("Bearer python-key", python.Headers["Authorization"]);
                Assert.DoesNotContain("Authorization", python.AdditionalProperties.Keys);
            });
    }

    [Fact]
    public void CreateGeoTools_ReturnsOnlyMcpSrvFuncAppTool()
    {
        RunWithMcpEnvironment(
            funcAppUrl: "https://func.example.com/",
            funcAppKey: "func-key",
            appServiceUrl: null,
            appServiceKey: null,
            pythonUrl: null,
            pythonKey: null,
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateGeoTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                var tool = Assert.Single(tools);
                Assert.Equal("McpSrvFuncApp", tool.ServerName);
                Assert.Equal("https://func.example.com/runtime/webhooks/mcp", tool.ServerAddress);
            });
    }

    [Fact]
    public void CreateNonAiWeatherTools_ReturnsMcpSrvAppServiceAndPythonTools()
    {
        RunWithMcpEnvironment(
            funcAppUrl: null,
            funcAppKey: null,
            appServiceUrl: "https://app.example.com/",
            appServiceKey: "app-key",
            pythonUrl: "https://python.example.com/",
            pythonKey: "python-key",
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateNonAiWeatherTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                Assert.Equal(2, tools.Count);

                var appService = Assert.Single(tools, tool => tool.ServerName == "McpSrvAppService");
                Assert.Equal("https://app.example.com/mcp", appService.ServerAddress);

                var python = Assert.Single(tools, tool => tool.ServerName == "McpSrvPython");
                Assert.Equal("https://python.example.com/mcp", python.ServerAddress);
            });
    }

    [Fact]
    public void CreateTools_ThrowsWhenMcpEnvironmentIsMissing()
    {
        RunWithMcpEnvironment(null, null, null, null, null, null, () =>
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
        string? pythonUrl,
        string? pythonKey,
        Action action)
    {
        var previousFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL");
        var previousFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY");
        var previousAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL");
        var previousAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY");
        var previousPythonUrl = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_URL");
        var previousPythonKey = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_KEY");
        try
        {
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_URL", funcAppUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY", funcAppKey);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL", appServiceUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY", appServiceKey);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_URL", pythonUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_KEY", pythonKey);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_URL", previousFuncAppUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY", previousFuncAppKey);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL", previousAppServiceUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY", previousAppServiceKey);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_URL", previousPythonUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_KEY", previousPythonKey);
        }
    }
}
