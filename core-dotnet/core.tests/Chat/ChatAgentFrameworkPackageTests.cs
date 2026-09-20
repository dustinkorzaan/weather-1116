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
    public void ExtensionsAi_StaysOnAgentFramework_1_22_0_Train()
    {
        var abstractions = typeof(AIFunctionFactory).Assembly.GetName();
        var extensionsAi = typeof(ChatClientBuilder).Assembly.GetName();
        var agentsAi = typeof(AIAgent).Assembly.GetName();
        var agentsOpenAi = Assembly.Load("Microsoft.Agents.AI.OpenAI").GetName();
        var openai = typeof(OpenAIClientOptions).Assembly.GetName();
        var adapter = Assembly.Load("Microsoft.Extensions.AI.OpenAI").GetName();

        Assert.Equal("Microsoft.Extensions.AI.Abstractions", abstractions.Name);
        Assert.Equal(new Version(10, 10, 0, 0), abstractions.Version);
        Assert.Equal("Microsoft.Extensions.AI", extensionsAi.Name);
        Assert.Equal(new Version(10, 10, 0, 0), extensionsAi.Version);
        Assert.Equal(new Version(1, 22, 0, 0), agentsAi.Version);
        Assert.Equal("Microsoft.Agents.AI.OpenAI", agentsOpenAi.Name);
        Assert.Equal(new Version(1, 22, 0, 0), agentsOpenAi.Version);
        Assert.Equal("Microsoft.Extensions.AI.OpenAI", adapter.Name);
        Assert.Equal(new Version(10, 10, 0, 0), adapter.Version);
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
        Assert.Equal(new Version(10, 10, 0, 0), adapter.Version);
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
    public async Task AsAIAgent_RunAsync_ConvertsHostedMcpApprovalPolicy_WithoutTypeLoadFailure()
    {
        // The adapter converts HostedMcpServerTool.ApprovalMode into OpenAI's
        // McpToolCallApprovalPolicy while building the request, before any bytes
        // hit the wire. A version mismatch between the OpenAI package and the
        // Microsoft.Extensions.AI.OpenAI adapter it was compiled against surfaces
        // here as a TypeLoadException/MissingMethodException, not as a network error.
        IList<AITool> mcpTools =
        [
            new HostedMcpServerTool("McpSrvFuncApp", new Uri("https://func.example.com/runtime/webhooks/mcp"))
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            },
        ];

        var agent = CreateResponsesClient().AsAIAgent(
            name: "Chat2b",
            instructions: "You are a test weather assistant.",
            model: "gpt-test",
            tools: mcpTools);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var ex = await Record.ExceptionAsync(() => agent.RunAsync("hello", cancellationToken: cts.Token));

        Assert.NotNull(ex);
        Assert.DoesNotContain(
            FlattenExceptions(ex!),
            e => e is TypeLoadException or MissingMethodException
                || (e.Message?.Contains("McpToolCallApprovalPolicy", StringComparison.Ordinal) ?? false));
    }

    private static IEnumerable<Exception> FlattenExceptions(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions.SelectMany(FlattenExceptions))
            {
                yield return inner;
            }
        }
        else if (exception.InnerException is not null)
        {
            foreach (var inner in FlattenExceptions(exception.InnerException))
            {
                yield return inner;
            }
        }
    }

    [Fact]
    public void AsAIFunction_WrapsSubAgent_ForOrchestrationDelegation()
    {
        var client = CreateResponsesClient();
        var geoAgent = client.AsAIAgent(
            name: "Geo",
            instructions: "You are a test geo assistant.",
            model: "gpt-test",
            tools: [AIFunctionFactory.Create(GetLatLong)]);

        var geoTool = geoAgent.AsAIFunction(new AIFunctionFactoryOptions
        {
            Name = "Geo",
            Description = "Geo assistant.",
        });

        Assert.Equal("Geo", geoTool.Name);
        Assert.Equal("Geo assistant.", geoTool.Description);
        Assert.IsAssignableFrom<AITool>(geoTool);
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
