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
/// Console V5). Instructions, response schema, and MCP tools are configured on the agent
/// itself (named by <c>AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME</c>) — this handler sends only the
/// user prompt, so there is no local schema, tool wiring, or tool-call loop to drive here.
/// Unlike V3/V4, this handler cannot strip <c>runLogDetails</c> from the schema the model
/// sees (there is no local schema to edit): the agent's own response schema must already
/// match <see cref="AIWeatherResponse"/>'s camelCase fields and must not require
/// <c>runLogDetails</c>, or deserialization can succeed with empty weather fields.
/// </summary>
public class GetCurrentAIWeatherV5Handler : IRequestHandler<GetCurrentAIWeatherV5Event, AIWeatherResponse>
{
    private static readonly string DefaultLocation = "Nashville, TN";

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

        CreateResponseOptions options = new()
        {
            InputItems =
            {
                ResponseItem.CreateUserMessageItem(userPrompt),
            },
        };

        // The hosted agent supplies instructions, response schema, and MCP tools itself, so a
        // single call is enough — like V4, there is no local tool-call loop to drive here.
        runLog.AddLog(toolLoopTurns, "Start CreateResponse", null);
        ResponseResult response = await client.CreateResponseAsync(options, cancellationToken);
        runLog.AddLog(toolLoopTurns, "Finish CreateResponse", response);

        if (response.Status != ResponseStatus.Completed)
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
