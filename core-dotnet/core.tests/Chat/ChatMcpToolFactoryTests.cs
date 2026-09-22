using Core.Chat.Services;

namespace Core.Tests.Chat;

[Collection(McpEnvironmentCollection.Name)]
public class ChatMcpToolFactoryTests
{
    [Fact]
    public void CreateTools_ReturnsThreeMcpToolsWithAuthOnHeaders()
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
                var (geoMcpTools, weatherMcpTools, weatherPythonMcpTools) = new ChatMcpToolFactory().CreateTools();

                Assert.Equal("McpSrvFuncApp", geoMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://func.example.com/runtime/webhooks/mcp"), geoMcpTools.ServerUri);
                Assert.Equal("func-key", geoMcpTools.Headers["x-functions-key"]);

                Assert.Equal("McpSrvAppService", weatherMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://app.example.com/mcp"), weatherMcpTools.ServerUri);
                Assert.Equal("Bearer app-key", weatherMcpTools.Headers["Authorization"]);

                Assert.Equal("McpSrvPython", weatherPythonMcpTools.ServerLabel);
                Assert.Equal(new Uri("https://python.example.com/mcp"), weatherPythonMcpTools.ServerUri);
                Assert.Equal("Bearer python-key", weatherPythonMcpTools.Headers["Authorization"]);
            });
    }

    [Fact]
    public void CreateTools_ThrowsWhenMcpEnvironmentIsMissing()
    {
        RunWithMcpEnvironment(null, null, null, null, null, null, () =>
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
