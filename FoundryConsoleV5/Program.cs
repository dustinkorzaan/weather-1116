extern alias AzIdentity;
using DefaultAzureCredential = AzIdentity::Azure.Identity.DefaultAzureCredential;

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Json;
using Core.Weather;
using DotNetEnv;
using OpenAI.Responses;
using System;
using System.Text.Json;
using System.Threading.Tasks;

#pragma warning disable OPENAI001

internal class Program
{
	private static async Task Main(string[] args)
	{
		Env.TraversePath().Load();

		var location = "Nashville, TN";
		await AskFoundryAgent(location);
	}

	private static async Task AskFoundryAgent(string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 5
		 - Ask Foundry Agent "What is today's weather in {location}?"
		 - Hosted Microsoft Foundry Agent (AIProjectClient + ProjectResponsesClient)
		 - Instructions, response schema, and MCP tools are configured on the agent
		 - This console sends the user prompt
		 - JSON output from AI
		""");

		var endpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(
			Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
			?? "https://wx1116prod2th7yydhws5h6.services.ai.azure.com/api/projects/wx1116-prod-proj").ToString();
		var agentName = "wx1116-agent-for-current-weather";
		var agentVersion = "1";

		var systemPrompt = """
		# Role & Operational Rules
		You are a dedicated weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
		You have access to tools for location mapping and real-time public meteorology data.

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

		var userPrompt = $"""
		What is today's weather in: {location}?
		""";

		Console.WriteLine($"Project endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName} (v{agentVersion})");

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: new DefaultAzureCredential());

		AgentReference agentReference = new(name: agentName, version: agentVersion);
		ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentReference);

		try
		{
			ResponseResult response = await responseClient.CreateResponseAsync(userPrompt);
			var content = response.GetOutputText();
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(content);

			if (aiWeather is null)
			{
				Console.WriteLine("Received empty or invalid JSON response.");
				Console.WriteLine("Raw output:");
				Console.WriteLine(string.IsNullOrWhiteSpace(content) ? "(empty)" : content);
			}
			else
			{
				aiWeather.WindDirectionSourceDegrees =
					WeatherUnitConversion.NormalizeSourceDegrees(aiWeather.WindDirectionSourceDegrees);
				aiWeather.WindDirectionSource =
					WeatherUnitConversion.DegreesToCompass(aiWeather.WindDirectionSourceDegrees);
				Console.WriteLine("\nResponse:");
				Console.WriteLine(JsonSerializer.Serialize(aiWeather, JsonDefaults.Pretty));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
			if (ex.InnerException is not null)
			{
				Console.WriteLine($"Inner: {ex.InnerException.Message}");
			}
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}
}
