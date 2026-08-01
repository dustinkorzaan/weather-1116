using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
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
	private readonly ILogger<GetCurrentAIWeatherHandler> _logger;

	public GetCurrentAIWeatherHandler(ILogger<GetCurrentAIWeatherHandler> logger)
	{
		_logger = logger;
	}

	public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherEvent request, CancellationToken cancellationToken)
	{
		var location = string.IsNullOrWhiteSpace(request.Location)
			? "Nashville, TN"
			: request.Location.Trim();

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		// Same field list / shape as V2's last example. Kept in the prompt because
		// CreateResponseOptions.TextOptions (text.format) is rejected when an agent is specified.
		var aiOutputSchema = """
		{
		  "type": "object",
		  "properties": {
		    "fullSummary": { "type": "string" },
		    "temperatureF": { "type": "number" },
		    "windSpeedMPH": { "type": "number" },
		    "windDirection": { "type": "string" },
		    "conditions": { "type": "string" }
		  },
		  "required": ["fullSummary", "temperatureF", "windSpeedMPH", "windDirection", "conditions"],
		  "additionalProperties": false
		}
		""";

		var systemPrompt = $"""
		You are a weather assistant. Use U.S. customary units (Fahrenheit, MPH).
		Use your tools to resolve the place name to latitude/longitude and to fetch current public weather for those coordinates.

		Reply with only JSON matching this schema — no text outside the JSON, no follow-up questions or offers:
		{aiOutputSchema}

		fullSummary: one sentence of the current weather facts only (temperature, wind speed, wind direction, conditions), using whichever place name is more user-friendly — the user entered location or the geo tool response "name" (prefer a clear city name over a raw ZIP or opaque code).
		""";

		var userPrompt = $"What is the current weather in: `{location}`?";

		_logger.LogInformation("AI Weather: Project endpoint {Endpoint}, Agent {Agent}", endpoint, agentName);
		_logger.LogInformation("AI Weather: System prompt for {Location}: {Prompt}", location, systemPrompt);
		_logger.LogInformation("AI Weather: User prompt for {Location}: {Prompt}", location, userPrompt);

		// Same client surface as Foundry sandbox (projectClient.OpenAI), auth with api-key like V1–V3.
		// ApiKey client path needs /openai/v1 on the project endpoint (avoids missing api-version).
		ProjectOpenAIClient projectOpenAIClient = new(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1"),
			});

		// Name only — Foundry uses the agent's current default version.
		ProjectResponsesClient responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		CreateResponseOptions options = new()
		{
			Instructions = systemPrompt,
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		ResponseResult response = await responseClient.CreateResponseAsync(options, cancellationToken);
		var content = response.GetOutputText();
		var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
			content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		if (aiWeather is null)
		{
			throw new InvalidOperationException(
				$"Foundry Agent returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
		}

		return aiWeather;
	}
}
