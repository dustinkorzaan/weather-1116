using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Json;
using static Core.AIWeather.Services.FoundryOpenAiEndpoint;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls the hosted model directly for current weather (same pattern as Foundry Console V3).
/// Geo and weather tools run in-process via <see cref="ChatToolExecutor"/>; the model
/// drives the tool-call loop, but no network hop leaves this process to resolve a tool call.
/// </summary>
public class GetCurrentAIWeatherHandler : IRequestHandler<GetCurrentAIWeatherEvent, AIWeatherResponse>
{
    private static readonly string DefaultLocation = "Nashville, TN";

    private readonly ChatToolExecutor _toolExecutor;
    private readonly ILogger<GetCurrentAIWeatherHandler> _logger;

    public GetCurrentAIWeatherHandler(ChatToolExecutor toolExecutor, ILogger<GetCurrentAIWeatherHandler> logger)
    {
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherEvent request, CancellationToken cancellationToken)
    {
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

        var systemPrompt = """
        # Role & Operational Rules
        You are a dedicated weather assistant.
        Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
        You have access to tools for location mapping and real-time public meteorology data.

        # Tool Protocol
        1. When given a location, immediately call your coordinates resolution tool. It returns ranked matches (rank 1 is best); pick the place that matches using name, state, and country — you may skip rank 1.
        2. Use those resolved coordinates to invoke your weather fetching tool.
        3. You must query these tools whenever real weather data is required to fulfill the request.

        # Constraints
        - Output raw JSON text only.
        - Do not wrap the JSON document in markdown code fences (do not wrap in ```json).
        - GitHub-flavored Markdown is allowed inside the fullSummary string when it makes the summary easier to read. Do not emit raw HTML.
        - Do not include any conversational pleasantries, introductory text, explanations, or trailing remarks.
        - Do not ask follow-up questions or offer further assistance.

        # JSON Structure Properties
        - fullSummary: One or two friendly sentences describing the current weather. Include the place name, temperature, wind speed, wind direction, and overall conditions. Keep those facts in the summary even though temperature, wind, and conditions are also JSON fields. Do not include latitude or longitude in fullSummary.
        - For the place name, prefer a clean, human-friendly city name from your geo tool over a ZIP code, coordinate pair, or opaque user input.
        - temperatureF: Current temperature in Fahrenheit (convert from the weather tool).
        - windSpeedMPH: Current wind speed in miles per hour (convert from the weather tool).
        - windDirection: Compass point such as N, NE, or SW.
        - windDirectionDegrees: Meteorological wind direction in degrees from the weather tool (0–360).
        - conditions: Short current conditions phrase from the weather tool.
        - latitude: Decimal degrees from your coordinates tool (positive north, negative south).
        - longitude: Decimal degrees from your coordinates tool (positive east, negative west).
        """;

        var userPrompt = $"What is the current weather in: `{location}`?";

        var aiOutputSchema = BuildAIOutputSchema();

        _logger.LogInformation("AI Weather: OpenAI endpoint {Endpoint}, deployment {Deployment}", endpoint, deploymentName);
        _logger.LogInformation("AI Weather: System prompt for {Location}: {Prompt}", location, systemPrompt);
        _logger.LogInformation("AI Weather: User prompt for {Location}: {Prompt}", location, userPrompt);
        _logger.LogInformation("AI Weather: Output schema for {Location}: {Schema}", location, aiOutputSchema);

        ResponsesClient client = new(
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        FunctionTool getLatLongTool = ChatToolDefinitions.CreateGetLatLongTool();
        FunctionTool getPublicWeatherCurrentTool = ChatToolDefinitions.CreateGetPublicWeatherCurrentTool();

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateSystemMessageItem(systemPrompt),
            // Placing the dynamic query at the absolute end ensures the unchanging system instructions,
            // response schema, and tool schemas form a stable hash that qualifies for Azure OpenAI
            // Prompt Caching (1,024+ token threshold).
            ResponseItem.CreateUserMessageItem(userPrompt),
        };

        bool requiresAction;
        string? content = null;

        do
        {
            requiresAction = false;

            CreateResponseOptions options = new(deploymentName, inputItems)
            {
                Tools = { getLatLongTool, getPublicWeatherCurrentTool },
                TextOptions = new ResponseTextOptions
                {
                    TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                        jsonSchemaFormatName: "ai_weather_response",
                        jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
                        jsonSchemaIsStrict: true),
                },
            };

            ResponseResult response = await client.CreateResponseAsync(options, cancellationToken);

            var functionCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
            if (response.Status != ResponseStatus.Completed && functionCalls.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Model response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
                    $"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
                    $"error: {response.Error?.Message ?? "(none)"}");
            }

            inputItems.AddRange(response.OutputItems);

            foreach (FunctionCallResponseItem functionCall in functionCalls)
            {
                var functionOutput = await _toolExecutor.ExecuteAsync(functionCall, cancellationToken);
                inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
                requiresAction = true;
            }

            if (!requiresAction)
            {
                content = response.GetOutputText();
            }
        } while (requiresAction);

        var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
            content ?? throw new InvalidOperationException("Model finished without producing content."));

        if (aiWeather is null)
        {
            throw new InvalidOperationException(
                $"Model returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
        }

        return aiWeather;
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
                        node["required"] = new JsonArray(properties.Select(property => (JsonNode)property.Key).ToArray());
                        node["additionalProperties"] = false;
                    }

                    return schema;
                },
            });

        return schema.ToJsonString(JsonDefaults.Pretty);
    }
}
