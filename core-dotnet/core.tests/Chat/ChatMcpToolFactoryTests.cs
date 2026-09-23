using Core.Chat.Services;

namespace Core.Tests.Chat;

[Collection(McpEnvironmentCollection.Name)]
public class ChatMcpToolFactoryTests
{
    [Fact]
    public void CreateTools_ReturnsFourMcpToolsWithAuthOnHeaders()
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
                var (geoMcpTools, weatherMcpTools, weatherPythonMcpTools, weatherNodeMcpTools) = new ChatMcpToolFactory().CreateTools();

                Assert.Equal("McpSrvFuncApp", geoMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://func.example.com/runtime/webhooks/mcp"), geoMcpTools.ServerUri);
                Assert.Equal("func-key", geoMcpTools.Headers["x-functions-key"]);

                Assert.Equal("McpSrvAppService", weatherMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://app.example.com/mcp"), weatherMcpTools.ServerUri);
                Assert.Equal("Bearer app-key", weatherMcpTools.Headers["Authorization"]);

                Assert.Equal("McpSrvPython", weatherPythonMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://python.example.com/mcp"), weatherPythonMcpTools.ServerUri);
                Assert.Equal("Bearer python-key", weatherPythonMcpTools.Headers["Authorization"]);

                Assert.Equal("McpSrvNode", weatherNodeMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://node.example.com/mcp"), weatherNodeMcpTools.ServerUri);
                Assert.Equal("Bearer node-key", weatherNodeMcpTools.Headers["Authorization"]);
            });
    }

    [Fact]
    public void CreateTools_ThrowsWhenMcpEnvironmentIsMissing()
    {
        RunWithMcpEnvironment(null, null, null, null, null, null, null, null, () =>
        {
            var factory = new ChatMcpToolFactory();
            var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateTools());
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
