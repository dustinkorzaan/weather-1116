using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls the hosted Microsoft Foundry Agent for current weather (same pattern as Foundry Console V4).
/// The agent uses its configured geo/weather tools; this handler does not call geo directly.
/// </summary>
public class GetCurrentAIWeatherHandler : IRequestHandler<GetCurrentAIWeatherEvent, AIWeatherResponse>
{
	private static readonly string DefaultLocation = "Nashville, TN";

	private readonly ILogger<GetCurrentAIWeatherHandler> _logger;

	public GetCurrentAIWeatherHandler(ILogger<GetCurrentAIWeatherHandler> logger)
	{
		_logger = logger;
	}

	public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherEvent request, CancellationToken cancellationToken)
	{
		var location = string.IsNullOrWhiteSpace(request.Location)
			? DefaultLocation
			: request.Location.Trim();

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var systemPrompt = """
		You are a weather assistant. Use U.S. customary units (Fahrenheit, MPH).
		Use your tools to resolve the place name to latitude/longitude and to fetch current public weather for those coordinates.

		Reply with only JSON — no text outside the JSON, no follow-up questions or offers.

		fullSummary: one sentence of the current weather facts only (temperature, wind speed, wind direction, conditions), using whichever place name is more user-friendly — the user entered location or the geo tool response "name" (prefer a clear city name over a raw ZIP or opaque code).
		""";

		var aiOutputSchema = BuildAIOutputSchema();

		// Foundry rejects `instructions` and `text` when an agent is specified,
		// so the system prompt and the schema travel in the user message.
		var userPrompt = $"""
		{systemPrompt}

		What is the current weather in: `{location}`?

		Return valid JSON matching this schema exactly:
		{aiOutputSchema}
		""";

		_logger.LogInformation("AI Weather: Project endpoint {Endpoint}, Agent {Agent}", endpoint, agentName);
		_logger.LogInformation("AI Weather: System prompt for {Location}: {Prompt}", location, systemPrompt);
		_logger.LogInformation("AI Weather: User prompt for {Location}: {Prompt}", location, userPrompt);

		ProjectOpenAIClient projectOpenAIClient = new(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1"),
			});

		ProjectResponsesClient responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		CreateResponseOptions options = new()
		{
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		ResponseResult response = await responseClient.CreateResponseAsync(options, cancellationToken);

		if (response.Status != ResponseStatus.Completed)
		{
			throw new InvalidOperationException(
				$"Foundry Agent response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
				$"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
				$"error: {response.Error?.Message ?? "(none)"}");
		}

		var content = response.GetOutputText();
		var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(content);

		if (aiWeather is null)
		{
			throw new InvalidOperationException(
				$"Foundry Agent returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
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

		return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
	}
}
