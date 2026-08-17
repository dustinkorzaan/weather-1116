using Core.AIWeather.Models;
using Core.Json;
using DotNetEnv;
using OpenAI;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
	private static async Task Main(string[] args)
	{
		Env.TraversePath().Load();

		string location = "Nashville, TN";

		await GetWeatherWithMcpTools(location);
	}





	private static async Task GetWeatherWithMcpTools(string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 4
		 - Ask AI "What is the current weather in {location}?"
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - Tools target remote MCP servers instead of in-process tool callbacks
		 - The service calls the MCP servers, so there is no local tool-call loop
		 - JSON output from AI
		""");

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Do not use C, KPH, or MM.
		You can call your MCP tools to resolve a place name to latitude/longitude,
		and to fetch current public weather for those coordinates.
		Use those tools whenever you need real weather data.

		Return valid JSON with these fields:
		- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirection (string)
		- conditions (string)

		You only return valid JSON.
		""";
		var userPrompt = $"""
		What is the current weather today in: {location}?
		""";

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

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		Console.WriteLine("\nAI Output Schema:");
		Console.WriteLine(aiOutputSchema);

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new OpenAIClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var mcpSrvFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY") ?? throw new InvalidOperationException("MCP_SRV_FUNC_APP_KEY not found in environment variables.");
		var mcpSrvAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY") ?? throw new InvalidOperationException("MCP_SRV_APP_SERVICE_KEY not found in environment variables.");

		McpTool myMcpSrvFuncApp = ResponseTool.CreateMcpTool(
			serverLabel: "McpSrvFuncApp",
			serverUri: new Uri("https://weather1116-prod-mcp-srv-func-app-b3a6f0cmhqcya3bw.westus2-01.azurewebsites.net/runtime/webhooks/mcp"),
			headers: new Dictionary<string, string> { ["x-functions-key"] = mcpSrvFuncAppKey },
			toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

		McpTool myMcpSrvAppService = ResponseTool.CreateMcpTool(
			serverLabel: "McpSrvAppService",
			serverUri: new Uri("https://weather1116-prod-mcp-srv-app-service-gdaef6e5cndqb3du.westus2-01.azurewebsites.net/mcp"),
			headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {mcpSrvAppServiceKey}" },
			toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

		Console.WriteLine($"\nMCP Servers:\n{myMcpSrvFuncApp.ServerLabel} {myMcpSrvFuncApp.ServerUri}\n{myMcpSrvAppService.ServerLabel} {myMcpSrvAppService.ServerUri}");

		var inputItems = new List<ResponseItem>()
		{
			ResponseItem.CreateSystemMessageItem(systemPrompt),
			ResponseItem.CreateUserMessageItem(userPrompt),
		};

		CreateResponseOptions options = new(deploymentName, inputItems)
		{
			Tools = { myMcpSrvFuncApp, myMcpSrvAppService },
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
			ResponseResult response = await client.CreateResponseAsync(options);

			var content = response.GetOutputText();
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(content);

			if (aiWeather is null)
			{
				Console.WriteLine("Received empty or invalid JSON response.");
			}
			else
			{
				Console.WriteLine("\nResponse:");
				Console.WriteLine(JsonSerializer.Serialize(aiWeather, JsonDefaults.Pretty));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}
}
