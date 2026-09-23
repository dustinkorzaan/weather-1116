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
            nodeUrl: "https://node.example.com/",
            nodeKey: "node-key",
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                Assert.Equal(4, tools.Count);

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

                var node = Assert.Single(tools, tool => tool.ServerName == "McpSrvNode");
                Assert.Equal("https://node.example.com/mcp", node.ServerAddress);
                Assert.Equal(HostedMcpServerToolApprovalMode.NeverRequire, node.ApprovalMode);
                Assert.NotNull(node.Headers);
                Assert.Equal("Bearer node-key", node.Headers["Authorization"]);
                Assert.DoesNotContain("Authorization", node.AdditionalProperties.Keys);
            });
    }

    [Fact]
    public void CreateGeoTools_ReturnsMcpSrvFuncAppAndPythonTools()
    {
        RunWithMcpEnvironment(
            funcAppUrl: "https://func.example.com/",
            funcAppKey: "func-key",
            appServiceUrl: null,
            appServiceKey: null,
            pythonUrl: "https://python.example.com/",
            pythonKey: "python-key",
            nodeUrl: null,
            nodeKey: null,
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateGeoTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                Assert.Equal(2, tools.Count);

                var funcApp = Assert.Single(tools, tool => tool.ServerName == "McpSrvFuncApp");
                Assert.Equal("https://func.example.com/runtime/webhooks/mcp", funcApp.ServerAddress);

                var python = Assert.Single(tools, tool => tool.ServerName == "McpSrvPython");
                Assert.Equal("https://python.example.com/mcp", python.ServerAddress);
                Assert.Equal("Bearer python-key", python.Headers!["Authorization"]);
            });
    }

    [Fact]
    public void CreateNonAiWeatherTools_ReturnsOnlyMcpSrvNodeTool()
    {
        RunWithMcpEnvironment(
            funcAppUrl: null,
            funcAppKey: null,
            appServiceUrl: null,
            appServiceKey: null,
            pythonUrl: null,
            pythonKey: null,
            nodeUrl: "https://node.example.com/",
            nodeKey: "node-key",
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateNonAiWeatherTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                var node = Assert.Single(tools);
                Assert.Equal("McpSrvNode", node.ServerName);
                Assert.Equal("https://node.example.com/mcp", node.ServerAddress);
            });
    }

    [Fact]
    public void CreateUserTools_ReturnsOnlyMcpSrvAppServiceTool()
    {
        RunWithMcpEnvironment(
            funcAppUrl: null,
            funcAppKey: null,
            appServiceUrl: "https://app.example.com/",
            appServiceKey: "app-key",
            pythonUrl: null,
            pythonKey: null,
            nodeUrl: null,
            nodeKey: null,
            () =>
            {
                var tools = new ChatHostedMcpToolFactory().CreateUserTools()
                    .Cast<HostedMcpServerTool>()
                    .ToList();

                var appService = Assert.Single(tools);
                Assert.Equal("McpSrvAppService", appService.ServerName);
                Assert.Equal("https://app.example.com/mcp", appService.ServerAddress);
                Assert.Equal("Bearer app-key", appService.Headers!["Authorization"]);
            });
    }

    [Fact]
    public void CreateTools_ThrowsWhenMcpEnvironmentIsMissing()
    {
        RunWithMcpEnvironment(null, null, null, null, null, null, null, null, () =>
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
        string? nodeUrl,
        string? nodeKey,
        Action action)
    {
        var previousFuncAppUrl = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_URL");
        var previousFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY");
        var previousAppServiceUrl = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL");
        var previousAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY");
        var previousPythonUrl = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_URL");
        var previousPythonKey = Environment.GetEnvironmentVariable("MCP_SRV_PYTHON_KEY");
        var previousNodeUrl = Environment.GetEnvironmentVariable("MCP_SRV_NODE_URL");
        var previousNodeKey = Environment.GetEnvironmentVariable("MCP_SRV_NODE_KEY");
        try
        {
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_URL", funcAppUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY", funcAppKey);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_URL", appServiceUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY", appServiceKey);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_URL", pythonUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_PYTHON_KEY", pythonKey);
            Environment.SetEnvironmentVariable("MCP_SRV_NODE_URL", nodeUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_NODE_KEY", nodeKey);
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
            Environment.SetEnvironmentVariable("MCP_SRV_NODE_URL", previousNodeUrl);
            Environment.SetEnvironmentVariable("MCP_SRV_NODE_KEY", previousNodeKey);
        }
    }
}
