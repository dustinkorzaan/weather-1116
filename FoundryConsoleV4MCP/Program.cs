using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using DotNetEnv;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
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
		""");

		var projectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJECT_ENDPOINT")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJECT_ENDPOINT. " +
				"Set it to your Foundry project endpoint, e.g. " +
				"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/<project-name>");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";

		var userPrompt = $"What is today's weather in {location}?";

		Console.WriteLine($"\nProject endpoint: {projectEndpoint}");
		Console.WriteLine($"Agent name: {agentName}");
		Console.WriteLine($"\nUser Prompt:\n{userPrompt}");

		ProjectOpenAIClient projectOpenAIClient = CreateProjectOpenAIClient(new Uri(projectEndpoint));
		ProjectResponsesClient responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

		try
		{
			ResponseResult response = await responseClient.CreateResponseAsync(userPrompt);
			var content = response.GetOutputText();

			Console.WriteLine("\nResponse:");
			Console.WriteLine(string.IsNullOrWhiteSpace(content) ? "(empty response)" : content);
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

	/// <summary>
	/// Prefer the same API-key env var used by V1–V3 when present; otherwise Entra ID via DefaultAzureCredential.
	/// Agent/project APIs often require Entra — set AZURE_FOUNDRY_PROD_EUS2_PROJECT_ENDPOINT and sign in (az login) if key auth fails.
	/// </summary>
	private static ProjectOpenAIClient CreateProjectOpenAIClient(Uri projectEndpoint)
	{
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY");
		if (!string.IsNullOrWhiteSpace(apiKey))
		{
			Console.WriteLine("Auth: AZURE_FOUNDRY_PROD_EUS2_KEY (api-key header)");
			return new ProjectOpenAIClient(
				ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
				new ProjectOpenAIClientOptions { Endpoint = projectEndpoint });
		}

		Console.WriteLine("Auth: DefaultAzureCredential (AZURE_FOUNDRY_PROD_EUS2_KEY not set)");
		return new ProjectOpenAIClient(projectEndpoint, new DefaultAzureCredential());
	}
}
