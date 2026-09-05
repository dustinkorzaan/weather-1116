using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Models;
using Core.Json;
using Core.Weather;
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

		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1"; // pragma: allowlist secret
		var agentName = "wx1116-agent-current-weather";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		var userPrompt = $"""
		What is today's weather in: {location}?
		""";

		Console.WriteLine($"OpenAI endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");
		Console.WriteLine("\nConfigured on the agent (not sent by this console):");
		Console.WriteLine("- Instructions");
		Console.WriteLine("- Response schema");
		Console.WriteLine("- MCP tools (lat/long + current weather)");
		Console.WriteLine($"\nUser Prompt (only input sent by this console):\n{userPrompt}");

		var projectOpenAIClient = new ProjectOpenAIClient(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri(endpoint),
			});

		var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		var options = new CreateResponseOptions()
		{
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		try
		{
			ResponseResult? response;
			// response = (await responseClient.CreateResponseAsync(options)).Value;

			Console.WriteLine("\nStreaming response:");
			response = null;

			await foreach (var update in responseClient.CreateResponseStreamingAsync(options))
			{
				if (update is StreamingResponseOutputTextDeltaUpdate delta)
				{
					Console.Write(delta.Delta);
				}
				else if (update is StreamingResponseCompletedUpdate completed)
				{
					response = completed.Response;
				}
				else if (update is StreamingResponseFailedUpdate failed)
				{
					response = failed.Response;
				}
			}

			Console.WriteLine();

			if (response is null)
			{
				Console.WriteLine("Streaming ended without a completed response.");
				Console.WriteLine("\nPress any key to continue.");
				Console.ReadKey(true);
				return;
			}

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
