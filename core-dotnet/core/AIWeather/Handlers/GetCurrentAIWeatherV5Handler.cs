using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Data.Domain;
using Core.Data.Events;
using Core.Weather;
using static Core.AIWeather.Services.FoundryOpenAiEndpoint;
using CQMediator;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls a hosted Microsoft Foundry Agent for current weather (same pattern as Foundry
/// Console V5). Instructions, response schema, and MCP tools are configured on the agent
/// itself (named by <c>AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME</c>) - this handler sends only the
/// user prompt, so there is no local schema, tool wiring, approval loop, or tool-call loop.
/// Each MCP tool on the agent must use <c>require_approval: never</c>; V5 will not round-trip
/// approvals (same as Chat3). Unlike V3/V4, this handler cannot strip <c>runLogDetails</c> from
/// the schema the model sees (there is no local schema to edit): the agent's own response
/// schema must already match <see cref="AIWeatherResponse"/>'s camelCase fields and must not
/// require <c>runLogDetails</c>, or deserialization can succeed with empty weather fields.
/// </summary>
public class GetCurrentAIWeatherV5Handler : IRequestHandler<GetCurrentAIWeatherV5Event, AIWeatherResponse>
{
    private const string Feature = "AIWeatherV5";
    private static readonly string DefaultLocation = "Nashville, TN";

    private readonly IMediator _mediator;
    private readonly ILogger<GetCurrentAIWeatherV5Handler> _logger;

    public GetCurrentAIWeatherV5Handler(IMediator mediator, ILogger<GetCurrentAIWeatherV5Handler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherV5Event request, CancellationToken cancellationToken)
    {
        var runLog = new AIRunLogRecorder();
        runLog.AddLog($"Start {nameof(GetCurrentAIWeatherV5Handler)}", null);

        void LogRunLogOnFailure(string reason) => _logger.LogWarning(
            "AI Weather run log at failure ({Reason}): {RunLog}",
            reason,
            JsonSerializer.Serialize(runLog.Hydrate()));

        var location = string.IsNullOrWhiteSpace(request.Location)
            ? DefaultLocation
            : request.Location.Trim();

        var activitySessionId = Guid.NewGuid().ToString();
        var traceId = Guid.NewGuid();
        var userPrompt = $"What is the current weather in: `{location}`?";
        var correlationId = await _mediator.Send(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            TraceId = traceId,
            Feature = Feature,
            FeatureCategory = AgentActivityFeatureCategory.Agent,
            SessionId = activitySessionId,
            Content = userPrompt,
            Location = location,
        }, cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        Task LogActivityErrorAsync(string errorMessage, ResponseResult? failureResponse) => _mediator.Send(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Response,
            TraceId = traceId,
            CorrelationId = correlationId,
            Feature = Feature,
            FeatureCategory = AgentActivityFeatureCategory.Agent,
            SessionId = activitySessionId,
            Location = location,
            InputTokenCount = failureResponse?.Usage?.InputTokenCount,
            CachedTokenCount = failureResponse?.Usage?.InputTokenDetails?.CachedTokenCount,
            OutputTokenCount = failureResponse?.Usage?.OutputTokenCount,
            ReasoningTokenCount = failureResponse?.Usage?.OutputTokenDetails?.ReasoningTokenCount,
            TotalTokenCount = failureResponse?.Usage?.TotalTokenCount,
            RuntimeMs = (int)stopwatch.ElapsedMilliseconds,
            ErrorMessage = errorMessage,
        }, cancellationToken);

        var endpoint = Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL."));

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_KEY.");

        var agentNameEnv = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME");
        var agentName = string.IsNullOrWhiteSpace(agentNameEnv) ? "wx1116-agent-for-current-weather" : agentNameEnv;

        _logger.LogInformation("AI Weather: OpenAI endpoint {Endpoint}, agent {Agent}", endpoint, agentName);

        ProjectOpenAIClient projectOpenAIClient = new(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        ProjectResponsesClient client = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

        CreateResponseOptions options = new()
        {
            InputItems =
            {
                ResponseItem.CreateUserMessageItem(userPrompt),
            },
        };

        // The hosted agent supplies instructions, response schema, and MCP tools itself, so a
        // single call is enough - like V4, there is no local tool-call loop to drive here.
        runLog.AddLog("Start CreateResponse", null);
        ResponseResult response = await client.CreateResponseAsync(options, cancellationToken);
        runLog.AddLog("Finish CreateResponse", response);

        var approvalRequests = response.OutputItems.OfType<McpToolCallApprovalRequestItem>().ToList();
        if (approvalRequests.Count > 0)
        {
            LogRunLogOnFailure("hosted agent requested MCP tool approval");
            var tools = string.Join(", ", approvalRequests.Select(item => $"{item.ServerLabel}/{item.ToolName}"));
            var message =
                $"Hosted agent '{agentName}' requested MCP tool approval ({tools}). " +
                "V5 sends only the user prompt and does not round-trip approvals. " +
                "On the agent, set each MCP tool to require_approval: never " +
                "(Foundry portal: Agents → this agent → Tools → MCP → Approval = Never), then publish a new version.";
            await LogActivityErrorAsync(message, response);
            throw new InvalidOperationException(message);
        }

        if (response.Status != ResponseStatus.Completed)
        {
            LogRunLogOnFailure("model response did not complete");
            var message =
                $"Model response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
                $"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
                $"error: {response.Error?.Message ?? "(none)"}";
            await LogActivityErrorAsync(message, response);
            throw new InvalidOperationException(message);
        }

        var content = response.GetOutputText();
        var modelOutput = JsonSerializer.Deserialize<AIWeatherResponse>(content);

        if (modelOutput is null)
        {
            LogRunLogOnFailure("model returned empty or invalid JSON");
            var message =
                $"Model returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}";
            await LogActivityErrorAsync(message, response);
            throw new InvalidOperationException(message);
        }

        modelOutput.WindDirectionSourceDegrees =
            WeatherUnitConversion.NormalizeSourceDegrees(modelOutput.WindDirectionSourceDegrees);
        modelOutput.WindDirectionSource =
            WeatherUnitConversion.DegreesToCompass(modelOutput.WindDirectionSourceDegrees);

        runLog.AddLog($"Finish {nameof(GetCurrentAIWeatherV5Handler)}", null);
        modelOutput.RunLogDetails = runLog.Hydrate();

        await _mediator.Send(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Response,
            TraceId = traceId,
            CorrelationId = correlationId,
            Feature = Feature,
            FeatureCategory = AgentActivityFeatureCategory.Agent,
            SessionId = activitySessionId,
            Content = content,
            Location = location,
            InputTokenCount = response.Usage?.InputTokenCount,
            CachedTokenCount = response.Usage?.InputTokenDetails?.CachedTokenCount,
            OutputTokenCount = response.Usage?.OutputTokenCount,
            ReasoningTokenCount = response.Usage?.OutputTokenDetails?.ReasoningTokenCount,
            TotalTokenCount = response.Usage?.TotalTokenCount,
            RuntimeMs = (int)stopwatch.ElapsedMilliseconds,
        }, cancellationToken);

        return modelOutput;
    }
}
