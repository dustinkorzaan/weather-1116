using Azure.AI.Extensions.OpenAI;
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

		// Same shape as the Foundry sandbox sample, with AZURE_FOUNDRY_PROD_EUS2_* settings.
		var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException(
				"Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL. " +
				"Expected e.g. https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2");

		var agentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME")
			?? "wx1116-agent-default";
		var agentVersion = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_AGENT_VERSION")
			?? "7";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var userPrompt = $"What is today's weather in {location}?";

		Console.WriteLine($"\nProject endpoint: {endpoint}");
		Console.WriteLine($"Agent: {agentName} (version {agentVersion})");
		Console.WriteLine($"\nUser Prompt:\n{userPrompt}");

		// Sandbox sample uses:
		//   AIProjectClient + DefaultAzureCredential → projectClient.OpenAI / ProjectOpenAIClient
		// AIProjectClient is Entra-only; for api-key we construct the same ProjectOpenAIClient
		// the sandbox reaches via projectClient.OpenAI.
		// ApiKey AuthenticationPolicy does not rewrite to /openai/v1 (TokenProvider/AIProjectClient does),
		// so append that segment — otherwise the service returns "Missing required query parameter: api-version".
		ProjectOpenAIClient projectOpenAIClient = new(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1"),
				ApiVersion = "v1",
			});

		AgentReference agentReference = new(name: agentName, version: agentVersion);
		ProjectResponsesClient responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentReference);

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
}
