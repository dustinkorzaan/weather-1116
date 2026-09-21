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
		var projectOpenAIClient = new ProjectOpenAIClient(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = projectEndpoint,
			});

		// Hosted-agent Responses calls need a real Foundry conversation id (not a random GUID).
		// V5 does not send a local system prompt — instructions live on the agent (see V4 for
		// model-direct systemPrompt + CreateSystemMessageItem).
		ConversationResource conversation = (await projectOpenAIClient
			.GetProjectConversationsClient()
			.CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

		ProjectResponsesClient responseClient =
			projectOpenAIClient.GetProjectResponsesClientForAgent(agentName, conversation.Id);

		var options = new CreateResponseOptions()
		{
			ConversationOptions = new ResponseConversationOptions(),
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		// Same SDK workaround as Chat3/V5 handler: non-null ConversationOptions plus a real
		// conversation id so ApplyClientDefaults does not corrupt the options patch.
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
