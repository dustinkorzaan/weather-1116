using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Models;
using DotNetEnv;
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
		 - JSON output from AI (prompt-shaped; Responses text.format is not allowed with agents)
		""");

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		// Same intent as V3's system prompt. CreateResponseOptions.Instructions is rejected when an
		// agent is specified, so this is folded into the user message (and printed for the demo).
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		Use your tools to resolve a place name to latitude/longitude and to fetch current public weather for those coordinates whenever you need real weather data.
		""";

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

		var userPrompt = $"""
		{systemPrompt.Trim()}

		What is today's weather in {location}?
		Use your tools to look up coordinates and current weather.

		Return valid JSON matching this schema exactly:
		{aiOutputSchema}

		Field notes:
		- fullSummary (string): full sentence of current weather including temperature, wind speed, wind direction, and conditions
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirection (string)
		- conditions (string)

		Use {location} as the location context.
		You only return valid JSON.
		Do not include any text outside the JSON.
		Do not ask follow-up questions or offer extra help (no "if you want", "I can also", hour-by-hour offers, etc.).
		The fullSummary field must state only the current weather facts — nothing conversational after that.
		""";

		Console.WriteLine($"\nProject endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");
		Console.WriteLine("\nSystem Prompt (included in user message; Instructions not allowed with agents):");
		Console.WriteLine(systemPrompt);
		Console.WriteLine($"\nUser Prompt:\n{userPrompt}");

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
			StreamingEnabled = true,
		};

		try
		{
			Console.WriteLine("\nStreaming agent response...");
			ResponseResult? response = null;

			await foreach (StreamingResponseUpdate streamUpdate in responseClient.CreateResponseStreamingAsync(options))
			{
				switch (streamUpdate)
				{
					case StreamingResponseCreatedUpdate:
						Console.WriteLine("[status] Request accepted by Foundry Agent...");
						break;
					case StreamingResponseQueuedUpdate:
						Console.WriteLine("[status] Request queued...");
						break;
					case StreamingResponseInProgressUpdate:
						Console.WriteLine("[status] Agent is working...");
						break;
					case StreamingResponseMcpListToolsInProgressUpdate:
						Console.WriteLine("[status] Agent is discovering tools...");
						break;
					case StreamingResponseMcpListToolsCompletedUpdate:
						Console.WriteLine("[status] Tool discovery completed.");
						break;
					case StreamingResponseMcpCallInProgressUpdate:
						Console.WriteLine("[status] Agent is calling a weather tool...");
						break;
					case StreamingResponseMcpCallCompletedUpdate:
						Console.WriteLine("[status] Weather tool call completed.");
						break;
					case StreamingResponseMcpCallFailedUpdate:
						Console.WriteLine("[status] A weather tool call failed.");
						break;
					case StreamingResponseOutputItemAddedUpdate { Item: McpToolCallItem mcpCall }:
						Console.WriteLine($"[tool] Agent requested: {mcpCall.ToolName}");
						break;
					case StreamingResponseOutputItemDoneUpdate { Item: McpToolCallItem mcpCall }:
						Console.WriteLine($"[tool] Finished: {mcpCall.ToolName}");
						break;
					case StreamingResponseOutputTextDeltaUpdate textDelta when !string.IsNullOrEmpty(textDelta.Delta):
						Console.Write(textDelta.Delta);
						break;
					case StreamingResponseCompletedUpdate completed:
						response = completed.Response;
						break;
					case StreamingResponseErrorUpdate errorUpdate:
						Console.WriteLine($"\n[error] {errorUpdate.Message}");
						break;
					case StreamingResponseFailedUpdate failedUpdate:
						Console.WriteLine($"\n[error] Agent request failed: {failedUpdate.Response?.Status}");
						break;
				}
			}

			Console.WriteLine();

			if (response is null)
			{
				Console.WriteLine("Received empty response from Foundry Agent.");
				return;
			}

			var content = response.GetOutputText();
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				content,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			if (aiWeather is null)
			{
				Console.WriteLine("Received empty or invalid JSON response.");
				Console.WriteLine("Raw output:");
				Console.WriteLine(string.IsNullOrWhiteSpace(content) ? "(empty)" : content);
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
}
