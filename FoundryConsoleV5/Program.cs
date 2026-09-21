using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Models;
using Core.AIWeather.Services;
using Core.Json;
using Core.Weather;
using DotNetEnv;
using OpenAI.Responses;
using System;
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

		var projectEndpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(endpoint);
		ProjectResponsesClient responseClient = FoundryAgentResponsesClientFactory.CreateForAgent(agentName, projectEndpoint);

		var options = new CreateResponseOptions()
		{
			ConversationOptions = new ResponseConversationOptions(),
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		// ProjectResponsesClient reads AgentConversationId (via ApplyClientDefaults) before every
		// call, which walks into ConversationOptions.Patch and NullReferenceExceptions in
		// CreateResponseOptions.PropagateGet if ConversationOptions is left null (hence setting it
		// above). But if AgentConversationId still reads null afterward, ApplyClientDefaults writes
		// it back as null, which removes "$.conversation" - and that removal propagates onto
		// ConversationOptions' own patch in a way that throws a KeyNotFoundException
		// ("No value found at JSON path '$'") from ResponseConversationOptions' JSON writer the
		// next time this options object is serialized. Giving it a real value up front avoids that.
		options.AgentConversationId = Guid.NewGuid().ToString();

		try
		{
			ResponseResult? response;

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
