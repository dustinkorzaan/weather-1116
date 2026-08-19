using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Services;
using Core.AIWeather.Models;
using Core.Json;
using Core.Weather;
using DotNetEnv;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
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
		Example 5
		 - Ask Foundry Agent "What is today's weather in {location}?"
		 - Call a hosted Microsoft Foundry Agent (not the model directly)
		 - Instructions, response schema, and MCP tools are configured on the agent
		 - This console sends only the user prompt
		 - JSON output from AI
		""");

		var endpoint = FoundryOpenAiEndpoint.Resolve(Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL."));
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";

		var userPrompt = $"""
		What is today's weather in: {location}?
		""";

		Console.WriteLine($"\nProject endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");
		Console.WriteLine("\nConfigured on the agent (not sent by this console):");
		Console.WriteLine("- Instructions");
		Console.WriteLine("- Response schema");
		Console.WriteLine("- MCP tools (lat/long + current weather)");
		Console.WriteLine("- This console auto-approves MCP tool calls (same fallback as Chat3)");
		Console.WriteLine($"\nUser Prompt (only input sent by this console):\n{userPrompt}");

		ProjectOpenAIClient projectOpenAIClient = new(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = endpoint,
			});

		ProjectResponsesClient responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		try
		{
			var pendingApprovals = new List<McpToolCallApprovalRequestItem>();
			string? previousResponseId = null;
			var sendUserMessage = true;
			ResponseResult? response = null;

			while (true)
			{
				CreateResponseOptions options = new()
				{
					StoredOutputEnabled = true,
				};

				if (!string.IsNullOrWhiteSpace(previousResponseId))
				{
					options.PreviousResponseId = previousResponseId;
				}

				if (sendUserMessage)
				{
					options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));
					sendUserMessage = false;
				}
				else
				{
					foreach (var approvalRequest in pendingApprovals)
					{
						options.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(
							approvalRequestId: approvalRequest.Id,
							approved: true));
					}

					pendingApprovals.Clear();
				}

				response = await responseClient.CreateResponseAsync(options);

				if (!string.IsNullOrWhiteSpace(response.Id))
				{
					previousResponseId = response.Id;
				}

				foreach (ResponseItem item in response.OutputItems)
				{
					if (item is McpToolCallApprovalRequestItem approvalRequest)
					{
						Console.WriteLine($"Auto-approving MCP tool {approvalRequest.ToolName} on {approvalRequest.ServerLabel}");
						pendingApprovals.Add(approvalRequest);
					}
				}

				if (pendingApprovals.Count == 0)
				{
					break;
				}
			}

			var content = response!.GetOutputText();
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
