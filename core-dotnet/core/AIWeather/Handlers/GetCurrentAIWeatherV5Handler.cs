using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Weather;
using static Core.AIWeather.Services.FoundryOpenAiEndpoint;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls a hosted Microsoft Foundry Agent for current weather (same pattern as Foundry
/// Console V5 / Chat3). Instructions, response schema, and MCP tools are configured on the
/// agent itself (named by <c>AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME</c>) - this handler sends
/// only the user prompt, so there is no local schema or tool wiring. Hosted MCP tools may
/// still emit approval requests; this handler auto-approves them (same fallback as Chat3)
/// and continues until the agent returns JSON.
/// Unlike V3/V4, this handler cannot strip <c>runLogDetails</c> from the schema the model
/// sees (there is no local schema to edit): the agent's own response schema must already
/// match <see cref="AIWeatherResponse"/>'s camelCase fields and must not require
/// <c>runLogDetails</c>, or deserialization can succeed with empty weather fields.
/// </summary>
public class GetCurrentAIWeatherV5Handler : IRequestHandler<GetCurrentAIWeatherV5Event, AIWeatherResponse>
{
    private static readonly string DefaultLocation = "Nashville, TN";
    private const int MaxApprovalTurns = 32;

    private readonly ILogger<GetCurrentAIWeatherV5Handler> _logger;

    public GetCurrentAIWeatherV5Handler(ILogger<GetCurrentAIWeatherV5Handler> logger)
    {
        _logger = logger;
    }

    public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherV5Event request, CancellationToken cancellationToken)
    {
        var runLog = new AIRunLogRecorder();
        var toolLoopTurns = 0;
        runLog.AddLog(toolLoopTurns, $"Start {nameof(GetCurrentAIWeatherV5Handler)}", null);

        void LogRunLogOnFailure(string reason) => _logger.LogWarning(
            "AI Weather run log at failure ({Reason}): {RunLog}",
            reason,
            JsonSerializer.Serialize(runLog.HydrateRuntimes()));

        var location = string.IsNullOrWhiteSpace(request.Location)
            ? DefaultLocation
            : request.Location.Trim();

        var endpoint = Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL."));

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

        var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
            ?? "wx1116-agent-default";

        _logger.LogInformation("AI Weather: OpenAI endpoint {Endpoint}, agent {Agent}", endpoint, agentName);

        ProjectOpenAIClient projectOpenAIClient = new(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        ProjectResponsesClient client = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

        var userPrompt = $"What is the current weather in: `{location}`?";

        // Hosted MCP tools often require approval (Chat3 has the same fallback). Without this
        // loop, CreateResponseAsync returns as soon as the agent asks to call a tool instead of
        // after weather JSON is produced. StoredOutputEnabled is required so PreviousResponseId
        // can continue the approval round-trip.
        var pendingApprovals = new List<McpToolCallApprovalRequestItem>();
        string? previousResponseId = null;
        var sendUserMessage = true;
        ResponseResult? response = null;

        while (true)
        {
            if (toolLoopTurns >= MaxApprovalTurns)
            {
                LogRunLogOnFailure("MCP approval loop exceeded max turns");
                throw new InvalidOperationException(
                    $"Hosted agent MCP approval loop exceeded {MaxApprovalTurns} turns.");
            }

            CreateResponseOptions options = new()
            {
                StoredOutputEnabled = true,
            };

            if (!string.IsNullOrWhiteSpace(previousResponseId))
            {
                options.PreviousResponseId = previousResponseId;
            }

            if (sendUserMessage)
            {
                options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));
                sendUserMessage = false;
            }
            else
            {
                foreach (var approvalRequest in pendingApprovals)
                {
                    options.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(
                        approvalRequestId: approvalRequest.Id,
                        approved: true));
                }

                pendingApprovals.Clear();
            }

            runLog.AddLog(toolLoopTurns, "Start CreateResponse", null);
            response = await client.CreateResponseAsync(options, cancellationToken);
            runLog.AddLog(toolLoopTurns, "Finish CreateResponse", response);

            if (!string.IsNullOrWhiteSpace(response.Id))
            {
                previousResponseId = response.Id;
            }

            foreach (ResponseItem item in response.OutputItems)
            {
                if (item is McpToolCallApprovalRequestItem approvalRequest)
                {
                    _logger.LogInformation(
                        "V5 auto-approving MCP tool {ToolName} on {ServerLabel}",
                        approvalRequest.ToolName,
                        approvalRequest.ServerLabel);
                    pendingApprovals.Add(approvalRequest);
                }
            }

            if (pendingApprovals.Count == 0)
            {
                break;
            }

            toolLoopTurns++;
        }

        if (response!.Status != ResponseStatus.Completed)
        {
            LogRunLogOnFailure("model response did not complete");
            throw new InvalidOperationException(
                $"Model response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
                $"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
                $"error: {response.Error?.Message ?? "(none)"}");
        }

        var content = response.GetOutputText();
        var modelOutput = JsonSerializer.Deserialize<AIWeatherResponse>(content);

        if (modelOutput is null)
        {
            LogRunLogOnFailure("model returned empty or invalid JSON");
            throw new InvalidOperationException(
                $"Model returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
        }

        modelOutput.WindDirectionSourceDegrees =
            WeatherUnitConversion.NormalizeSourceDegrees(modelOutput.WindDirectionSourceDegrees);
        modelOutput.WindDirectionSource =
            WeatherUnitConversion.DegreesToCompass(modelOutput.WindDirectionSourceDegrees);

        runLog.AddLog(toolLoopTurns, $"Finish {nameof(GetCurrentAIWeatherV5Handler)}", null);
        modelOutput.RunLogDetails = runLog.HydrateRuntimes();

        return modelOutput;
    }
}
