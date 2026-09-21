using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Json;
using Core.Weather;
using DotNetEnv;
using OpenAI.Conversations;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Threading.Tasks;

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
		 - Call a hosted Microsoft Foundry Agent (not the model directly)
		 - Instructions, response schema, and MCP tools are configured on the agent
		 - This console sends only the user prompt
		 - JSON output from AI
		""");

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL") ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROD_PROJ_URL not found in environment variables.");
		var agentName = "wx1116-agent-for-current-weather";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		// Demo copy of wx1116-agent-for-current-weather (published on the agent — not sent by this console).
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

		var aiOutputSchema = """
		{
		  "name": "AIWeatherResponse",
		  "strict": true,
		  "schema": {
		    "type": "object",
		    "properties": {
		      "fullSummary": { "type": "string" },
		      "temperatureF": { "type": "number" },
		      "windSpeedMPH": { "type": "number" },
		      "windDirectionSource": { "type": "string" },
		      "windDirectionSourceDegrees": { "type": "integer" },
		      "conditions": { "type": "string" },
		      "latitude": { "type": "number" },
		      "longitude": { "type": "number" }
		    },
		    "required": [
		      "fullSummary",
		      "temperatureF",
		      "windSpeedMPH",
		      "windDirectionSource",
		      "windDirectionSourceDegrees",
		      "conditions",
		      "latitude",
		      "longitude"
		    ],
		    "additionalProperties": false
		  }
		}
		""";

		Console.WriteLine($"OpenAI endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");

		Console.WriteLine("\nSystem Prompt (on agent — demo text, not sent in this request):");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		Console.WriteLine("\nAI Output Schema (on agent — demo text, not sent in this request):");
		Console.WriteLine(aiOutputSchema);

		var projectEndpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(endpoint);
		var projectOpenAIClient = new ProjectOpenAIClient(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			FoundryOpenAiEndpoint.CreateProjectOpenAIClientOptions(projectEndpoint, agentName));

		ConversationResource conversation = (await projectOpenAIClient
			.GetProjectConversationsClient()
			.CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

		ProjectResponsesClient responseClient =
			projectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(agentName, conversation.Id);

		var options = new CreateResponseOptions()
		{
			ConversationOptions = new ResponseConversationOptions(),
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		options.AgentConversationId = conversation.Id;

		try
		{
			ResponseResult response = await responseClient.CreateResponseAsync(options);

			var requestedApproval = false;
			foreach (var item in response.OutputItems)
			{
				if (item is McpToolCallApprovalRequestItem)
				{
					requestedApproval = true;
					break;
				}
			}

			if (requestedApproval)
			{
				Console.WriteLine("The agent requested MCP tool approval. V5 does not round-trip approvals.");
				Console.WriteLine("Set each MCP tool on the agent to require_approval: never (Foundry portal: Agents → this agent → Tools → MCP → Approval = Never), then publish a new version.");
			}
			else
			{
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
		}
		catch (ClientResultException ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
			if (ex.InnerException is not null)
			{
				Console.WriteLine($"Inner: {ex.InnerException.Message}");
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
