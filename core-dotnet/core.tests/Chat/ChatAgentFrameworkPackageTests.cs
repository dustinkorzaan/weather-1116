using System.ClientModel;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

namespace Core.Tests.Chat;

public class ChatAgentFrameworkPackageTests
{
    [Fact]
    public void ExtensionsAi_StaysOnAgentFramework_1_20_0_Train()
    {
        var abstractions = typeof(AIFunctionFactory).Assembly.GetName();
        var extensionsAi = typeof(ChatClientBuilder).Assembly.GetName();
        var agentsAi = typeof(AIAgent).Assembly.GetName();
        var agentsOpenAi = Assembly.Load("Microsoft.Agents.AI.OpenAI").GetName();
        var openai = typeof(OpenAIClientOptions).Assembly.GetName();
        var adapter = Assembly.Load("Microsoft.Extensions.AI.OpenAI").GetName();

        Assert.Equal("Microsoft.Extensions.AI.Abstractions", abstractions.Name);
        Assert.Equal(new Version(10, 9, 0, 0), abstractions.Version);
        Assert.Equal("Microsoft.Extensions.AI", extensionsAi.Name);
        Assert.Equal(new Version(10, 9, 0, 0), extensionsAi.Version);
        Assert.Equal(new Version(1, 20, 0, 0), agentsAi.Version);
        Assert.Equal("Microsoft.Agents.AI.OpenAI", agentsOpenAi.Name);
        Assert.Equal(new Version(1, 20, 0, 0), agentsOpenAi.Version);
        Assert.Equal("Microsoft.Extensions.AI.OpenAI", adapter.Name);
        Assert.Equal(new Version(10, 9, 0, 0), adapter.Version);
        Assert.Equal("OpenAI", openai.Name);
        Assert.Equal(new Version(2, 13, 0, 0), openai.Version);
    }

    [Fact]
    public void AsAIAgent_CreatesChat2aStyleAgent_WithInProcessTools()
    {
        var agent = CreateResponsesClient().AsAIAgent(
            name: "Chat2a",
            instructions: "You are a test weather assistant.",
            model: "gpt-test",
            tools:
            [
                AIFunctionFactory.Create(GetLatLong),
            ]);

        Assert.NotNull(agent);
        Assert.Equal("Chat2a", agent.Name);

        var adapter = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName())
            .Single(name => name.Name == "Microsoft.Extensions.AI.OpenAI");
        Assert.Equal(new Version(10, 9, 0, 0), adapter.Version);
    }

    [Fact]
    public void AsAIAgent_CreatesChat2bStyleAgent_WithHostedMcpTools()
    {
        IList<AITool> mcpTools =
        [
            new HostedMcpServerTool("McpSrvFuncApp", new Uri("https://func.example.com/runtime/webhooks/mcp"))
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["x-functions-key"] = "func-key",
                },
            },
        ];

        var agent = CreateResponsesClient().AsAIAgent(
            name: "Chat2b",
            instructions: "You are a test weather assistant.",
            model: "gpt-test",
            tools: mcpTools);

        Assert.NotNull(agent);
        Assert.Equal("Chat2b", agent.Name);
    }

    [Fact]
    public void AIFunctionFactory_OmitsCancellationTokenFromToolSchema()
    {
        var function = AIFunctionFactory.Create(GetLatLong);
        var schema = function.JsonSchema.ToString();

        Assert.Contains("location", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cancellationToken", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CancellationToken", schema, StringComparison.Ordinal);
    }

    private static ResponsesClient CreateResponsesClient() => new(
        credential: new ApiKeyCredential("test-key"),
        options: new ResponsesClientOptions
        {
            Endpoint = new Uri("https://example.invalid/"),
        });

    [Description("Resolve a location name to latitude/longitude.")]
    private static Task<string> GetLatLong(
        [Description("City and optional region/country")] string location,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult(location);
    }
}
