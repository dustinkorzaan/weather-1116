using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Chat.Services;
using Core.Json;
using Core.Weather;
using static Core.AIWeather.Services.FoundryOpenAiEndpoint;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls the hosted model directly for current weather (same pattern as Foundry Console V4).
/// Remote MCP tools (resolved via <see cref="ChatMcpToolFactory"/>) resolve geo and weather
/// data; the model host executes those tool calls itself, so there is no local tool-call loop.
/// </summary>
public class GetCurrentAIWeatherV4Handler : IRequestHandler<GetCurrentAIWeatherV4Event, AIWeatherResponse>
{
    private static readonly string DefaultLocation = "Nashville, TN";

    private readonly ChatMcpToolFactory _mcpToolFactory;
    private readonly ILogger<GetCurrentAIWeatherV4Handler> _logger;

    public GetCurrentAIWeatherV4Handler(ChatMcpToolFactory mcpToolFactory, ILogger<GetCurrentAIWeatherV4Handler> logger)
    {
        _mcpToolFactory = mcpToolFactory;
        _logger = logger;
    }

    public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherV4Event request, CancellationToken cancellationToken)
    {
        var runLog = new AIRunLogRecorder();
        var toolLoopTurns = 0;
        runLog.AddLog(toolLoopTurns, $"Start {nameof(GetCurrentAIWeatherV4Handler)}", null);

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

        var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_MODEL.");

        var systemPrompt =
            """
            # Role & Operational Rules
            You are a dedicated weather assistant.
            Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
            You have access to 3rd-party Model Context Protocol (MCP) tools for location mapping and real-time public meteorology data.

            # Tool Protocol
            1. When given a location, immediately call your coordinates resolution tool. It returns ranked matches (rank 1 is best); select the single best-matching place using name, state, and country — normally rank 1, but you may skip rank 1 when a lower rank is clearly correct.
            2. Use the latitude and longitude from the best result (normally rank 1) to invoke your weather fetching tool. Fetch weather for that location only — do not query multiple matches.
            3. You must query these tools whenever real weather data is required to fulfill the request.

            # Constraints
            - Output raw JSON text only.
            - Do not wrap the JSON document in markdown code fences (do not wrap in ```json).
            - GitHub-flavored Markdown is allowed inside the fullSummary string when it makes the summary easier to read. Do not emit raw HTML.
            - Do not include any conversational pleasantries, introductory text, explanations, or trailing remarks.
            - Do not ask follow-up questions or offer further assistance.

            # JSON Structure Properties
            - fullSummary: One or two friendly sentences describing the current weather. Include the place name, temperature, wind speed, wind direction, and overall conditions. Keep those facts in the summary even though temperature, wind, and conditions are also JSON fields. Do not include latitude or longitude in fullSummary. When stating wind direction, use the meteorological source compass label from windDirectionSource (where the wind comes from), optionally with source degrees in parentheses (e.g. SW (224°)). Do not add 180 to degrees.
            - For the place name, prefer a clean, human-friendly city name from your geo tool over a ZIP code, coordinate pair, or opaque user input.
            - temperatureF: Current temperature in Fahrenheit (convert from the weather tool).
            - windSpeedMPH: Current wind speed in miles per hour (convert from the weather tool).
            - windDirectionSourceDegrees: Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
            - windDirectionSource: 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
            - conditions: Short current conditions phrase from the weather tool.
            - latitude: Decimal degrees from the best geo result (positive north, negative south).
            - longitude: Decimal degrees from the best geo result (positive east, negative west).
            """;

        var userPrompt = $"What is the current weather in: `{location}`?";

        var aiOutputSchema = BuildAIOutputSchema();

        _logger.LogInformation("AI Weather: OpenAI endpoint {Endpoint}, deployment {Deployment}", endpoint, deploymentName);

        ResponsesClient client = new(
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        var (getLatLongTool, getWeatherTool) = _mcpToolFactory.CreateTools();

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateSystemMessageItem(systemPrompt),
            // Placing the dynamic query at the absolute end ensures the unchanging system instructions,
            // response schema, and MCP tool schemas form a stable hash that qualifies for Azure OpenAI
            // Prompt Caching (1,024+ token threshold).
            ResponseItem.CreateUserMessageItem(userPrompt),
        };

        CreateResponseOptions options = new(deploymentName, inputItems)
        {
            Tools = { getLatLongTool, getWeatherTool },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "ai_weather_response",
                    jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
                    jsonSchemaIsStrict: true),
            },
        };

        // The remote MCP servers execute tool calls on the model host's side, so a single
        // call is enough — unlike V3, there is no local tool-call loop to drive here.
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

        runLog.AddLog(toolLoopTurns, $"Finish {nameof(GetCurrentAIWeatherV4Handler)}", null);
        modelOutput.RunLogDetails = runLog.HydrateRuntimes();

        return modelOutput;
    }

    private static string BuildAIOutputSchema()
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            JsonSerializerOptions.Default,
            typeof(AIWeatherResponse),
            new JsonSchemaExporterOptions
            {
                TreatNullObliviousAsNonNullable = true,
                TransformSchemaNode = static (context, schema) =>
                {
                    if (schema is JsonObject node && node["properties"] is JsonObject properties)
                    {
                        properties.Remove("runLogDetails");
                        node["required"] = new JsonArray(properties.Select(property => (JsonNode)property.Key).ToArray());
                        node["additionalProperties"] = false;
                    }

                    return schema;
                },
            });

        return schema.ToJsonString(JsonDefaults.Pretty);
    }
}
