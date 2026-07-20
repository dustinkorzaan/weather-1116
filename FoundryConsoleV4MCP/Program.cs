using Azure.AI.Extensions.OpenAI;
using DotNetEnv;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

internal class Program
{
	private static async Task Main(string[] args)
	{
		Env.TraversePath().Load();

		string location = "Nashville, TN";
		await AskFoundryAgent(location);
	}

	private static async Task AskFoundryAgent(string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 4
		 - Ask Foundry Agent "What is today's weather in {location}?"
		 - Call a hosted Microsoft Foundry Agent (not a model directly)
		 - Agent uses its configured tools (lat/long + current weather)
		 - JSON output from AI (strict schema)
		""");

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var userPrompt = $"""
		What is today's weather in {location}?
		Use your tools to look up coordinates and current weather.

		Return valid JSON with these fields:
		- summary (string) (full sentence summary of the current weather including temperature, wind speed, wind direction, and conditions)
		- temperature (number)
		- windSpeed (number)
		- windDirection (string)
		- conditions (string)

		Use {location} as the location context.
		You only return valid JSON.
		""";

		var aiOutputSchema = """
		{
		  "type": "object",
		  "properties": {
		    "summary": { "type": "string" },
		    "temperature": { "type": "number" },
		    "windSpeed": { "type": "number" },
		    "windDirection": { "type": "string" },
		    "conditions": { "type": "string" }
		  },
		  "required": ["summary", "temperature", "windSpeed", "windDirection", "conditions"],
		  "additionalProperties": false
		}
		""";

		Console.WriteLine($"\nProject endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");
		Console.WriteLine($"\nUser Prompt:\n{userPrompt}");
		Console.WriteLine("\nAI Output Schema:");
		Console.WriteLine(aiOutputSchema);

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
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
			TextOptions = new ResponseTextOptions
			{
				TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "ai_weather_response",
					jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
					jsonSchemaIsStrict: true)
			}
		};

		try
		{
			ResponseResult response = await responseClient.CreateResponseAsync(options);
			var content = response.GetOutputText();
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				content,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			if (aiWeather is null)
			{
				Console.WriteLine("Received empty or invalid JSON response.");
			}
			else
			{
				Console.WriteLine("\nResponse:");
				Console.WriteLine(JsonSerializer.Serialize(aiWeather, new JsonSerializerOptions { WriteIndented = true }));
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

	public class AIWeatherResponse
	{
		[JsonPropertyName("summary")]
		public string Summary { get; set; } = string.Empty;

		[JsonPropertyName("temperature")]
		public double Temperature { get; set; }

		[JsonPropertyName("windSpeed")]
		public double WindSpeed { get; set; }

		[JsonPropertyName("windDirection")]
		public string WindDirection { get; set; } = string.Empty;

		[JsonPropertyName("conditions")]
		public string Conditions { get; set; } = string.Empty;
	}
}
