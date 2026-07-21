using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather;

internal static class FoundryAgentWeatherRequestFactory
{
	internal sealed record WeatherRequestContext(
		ProjectResponsesClient ResponseClient,
		string Location,
		string UserPrompt);

	internal static WeatherRequestContext Create(string location)
	{
		var normalizedLocation = string.IsNullOrWhiteSpace(location)
			? "Nashville, TN"
			: location.Trim();

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		Use your tools to resolve a place name to latitude/longitude and to fetch current public weather for those coordinates whenever you need real weather data.
		""";

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

		var userPrompt = $"""
		{systemPrompt.Trim()}

		What is the current weather in the user entered location: `{normalizedLocation}`?
		Use your tools to look up coordinates and current weather.
		When your geo tool returns a location, choose whichever place name is more user-friendly for fullSummary: the user entered location (`{normalizedLocation}`) or the geo tool response "name" (for example prefer a clear city name over a raw ZIP or opaque code).

		Return valid JSON matching this schema exactly:
		{aiOutputSchema}

		Field notes:
		- fullSummary (string): full sentence of current weather including temperature, wind speed, wind direction, and conditions
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirection (string)
		- conditions (string)

		Use the more user-friendly place name as the location context in fullSummary.
		You only return valid JSON.
		Do not include any text outside the JSON.
		Do not ask follow-up questions or offer extra help (no "if you want", "I can also", hour-by-hour offers, etc.).
		The fullSummary field must state only the current weather facts — nothing conversational after that.
		""";

		var projectOpenAIClient = new ProjectOpenAIClient(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1"),
			});

		var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		return new WeatherRequestContext(responseClient, normalizedLocation, userPrompt);
	}

	internal static CreateResponseOptions CreateOptions(string userPrompt, bool streaming)
	{
		var options = new CreateResponseOptions
		{
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		if (streaming)
		{
			options.StreamingEnabled = true;
		}

		return options;
	}
}
