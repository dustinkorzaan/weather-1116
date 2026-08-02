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
		Example 5
		 - Ask Foundry Agent "What is today's weather in {location}?"
		 - Call a hosted Microsoft Foundry Agent (not a model directly)
		 - Agent uses its configured tools (lat/long + current weather)
		 - JSON output from AI (prompt-shaped; Responses text.format is not allowed with agents)
		""");

		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL.");
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";

		var userPrompt = $"""
		What is today's weather in: {location}?
		""";

		Console.WriteLine($"\nProject endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName}");
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
		};

		try
		{
			ResponseResult response = await responseClient.CreateResponseAsync(options);
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
