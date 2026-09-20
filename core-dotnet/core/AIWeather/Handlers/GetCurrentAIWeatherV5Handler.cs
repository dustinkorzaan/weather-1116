using System.Diagnostics;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Core.Agent.Events;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Data.Domain;
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
    private const string Feature = nameof(GetCurrentAIWeatherV5Handler);
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
        var runId = Guid.NewGuid();
        var userPrompt = $"What is the current weather in: `{location}`?";
        var correlationId = await _mediator.Send(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            RunId = runId,
            Feature = Feature,
            FeatureCategory = AgentActivityFeatureCategory.Agent,
            SessionId = activitySessionId,
            Content = userPrompt,
            Location = location,
        }, cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        ResponseResult? response = null;

        // Every exit path below -- an explicit validation throw, or any exception raised by the
        // Foundry client itself (auth failure, network error, timeout) -- lands here, so the
        // Request row logged above always gets a paired Response row with ErrorMessage set
        // before the exception propagates. Log write failures are not caught here: they
        // propagate too, the same fail-closed policy as a missing DB_CONNECTION_STRING.
        try
        {
            var projectUrl = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
                ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL.");

            var agentNameEnv = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME");
            var agentName = string.IsNullOrWhiteSpace(agentNameEnv) ? "wx1116-agent-for-current-weather" : agentNameEnv;

            var projectEndpoint = ResolveProjectEndpoint(projectUrl);

            _logger.LogInformation(
                "AI Weather: Foundry project {ProjectEndpoint}, agent {Agent}",
                projectEndpoint,
                agentName);

            ProjectResponsesClient client = FoundryAgentResponsesClientFactory.CreateForAgent(agentName, projectEndpoint);

            CreateResponseOptions options = new()
            {
                ConversationOptions = new ResponseConversationOptions(),
                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(userPrompt),
                },
            };

            // ProjectResponsesClient.CreateResponseAsync reads AgentConversationId (via
            // ApplyClientDefaults) before every call. That getter walks into
            // ConversationOptions.Patch, so a bare CreateResponseOptions (ConversationOptions
            // left null) crashes with a NullReferenceException in
            // CreateResponseOptions.PropagateGet before any request is sent - hence
            // ConversationOptions above. But when AgentConversationId still reads null (no
            // conversation id set), ApplyClientDefaults writes it back as null, which removes
            // "$.conversation" and - because of how that removal propagates onto
            // ConversationOptions' own patch - throws a KeyNotFoundException
            // ("No value found at JSON path '$'") from ResponseConversationOptions' JSON writer
            // the next time this options object is serialized. Giving AgentConversationId a
            // real (non-null) value up front short-circuits ApplyClientDefaults's null-check
            // entirely, so it never touches the patch again. V5 has no multi-turn conversation
            // to resume, so this is just a stable per-run id, not a real Foundry conversation.
            options.AgentConversationId = activitySessionId;

            // The hosted agent supplies instructions, response schema, and MCP tools itself, so a
            // single call is enough - like V4, there is no local tool-call loop to drive here.
            runLog.AddLog("Start CreateResponse", null);
            response = await client.CreateResponseAsync(options, cancellationToken);
            runLog.AddLog("Finish CreateResponse", response);

            if (response is null)
            {
                LogRunLogOnFailure("CreateResponseAsync returned null response");
                throw new InvalidOperationException(
                    "Foundry agent returned no response. Check AZURE_FOUNDRY_PROD_PROJ_URL, agent name, and agent publish status.");
            }

            var approvalRequests = response.OutputItems.OfType<McpToolCallApprovalRequestItem>().ToList();
            if (approvalRequests.Count > 0)
            {
                LogRunLogOnFailure("hosted agent requested MCP tool approval");
                var tools = string.Join(", ", approvalRequests.Select(item => $"{item.ServerLabel}/{item.ToolName}"));
                throw new InvalidOperationException(
                    $"Hosted agent '{agentName}' requested MCP tool approval ({tools}). " +
                    "V5 sends only the user prompt and does not round-trip approvals. " +
                    "On the agent, set each MCP tool to require_approval: never " +
                    "(Foundry portal: Agents → this agent → Tools → MCP → Approval = Never), then publish a new version.");
            }

            if (response.Status != ResponseStatus.Completed)
            {
                LogRunLogOnFailure("model response did not complete");
                throw new InvalidOperationException(
                    $"Model response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
                    $"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
                    $"error: {response.Error?.Message ?? "(none)"}");
            }

            var content = response.GetOutputText();
            if (string.IsNullOrWhiteSpace(content))
            {
                LogRunLogOnFailure("model returned empty or invalid JSON");
                throw new InvalidOperationException(
                    "Model returned empty or invalid JSON. Raw output: (empty)");
            }

            var modelOutput = JsonSerializer.Deserialize<AIWeatherResponse>(content);

            if (modelOutput is null)
            {
                LogRunLogOnFailure("model returned empty or invalid JSON");
                throw new InvalidOperationException(
                    $"Model returned empty or invalid JSON. Raw output: {content}");
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
                RunId = runId,
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
        catch (Exception ex)
        {
            await _mediator.Send(new LogAgentActivityEvent
            {
                Direction = AgentActivityDirection.Response,
                RunId = runId,
                CorrelationId = correlationId,
                Feature = Feature,
                FeatureCategory = AgentActivityFeatureCategory.Agent,
                SessionId = activitySessionId,
                Location = location,
                InputTokenCount = response?.Usage?.InputTokenCount,
                CachedTokenCount = response?.Usage?.InputTokenDetails?.CachedTokenCount,
                OutputTokenCount = response?.Usage?.OutputTokenCount,
                ReasoningTokenCount = response?.Usage?.OutputTokenDetails?.ReasoningTokenCount,
                TotalTokenCount = response?.Usage?.TotalTokenCount,
                RuntimeMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
            }, cancellationToken);
            throw;
        }
    }
}
