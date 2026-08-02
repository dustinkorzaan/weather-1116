using Core.AIWeather.Models;
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
		 - Tools come from remote MCP servers instead of in-process tool callbacks
		 - The service calls the MCP servers, so there is no local tool-call loop
		 - JSON output from AI
		""");

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		You can call your MCP tools to resolve a place name to latitude/longitude,
		and to fetch current public weather for those coordinates.
		Use those tools whenever you need real weather data.

		Return valid JSON with these fields:
		- fullSummary (string) (full sentence summary of the current weather including temperature, wind speed, wind direction, and conditions)
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

		// Placeholder — add the third party MCP servers this demo should call, e.g.
		// mcpTools.Add(ResponseTool.CreateMcpTool(
		//     serverLabel: "weather_mcp_dotnet",
		//     serverUri: new Uri("https://weather1116-prod-mcpapp.azurewebsites.net/mcp"),
		//     authorizationToken: Environment.GetEnvironmentVariable("MCP_API_KEY"),
		//     toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval)));
		var mcpTools = new List<ResponseTool>();

		Console.WriteLine($"\nMCP tools: {mcpTools.Count}");

		var inputItems = new List<ResponseItem>()
		{
			ResponseItem.CreateSystemMessageItem(systemPrompt),
			ResponseItem.CreateUserMessageItem(userPrompt),
		};

		CreateResponseOptions options = new(deploymentName, inputItems)
		{
			TextOptions = new ResponseTextOptions
			{
				TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "ai_weather_response",
					jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
					jsonSchemaIsStrict: true)
			}
		};

		foreach (var mcpTool in mcpTools)
		{
			options.Tools.Add(mcpTool);
		}

		try
		{
			ResponseResult response = await client.CreateResponseAsync(options);

			foreach (ResponseItem outputItem in response.OutputItems)
			{
				if (outputItem is McpToolCallItem mcpToolCall)
				{
					Console.WriteLine($"\nMCP tool call: {mcpToolCall.ServerLabel}.{mcpToolCall.ToolName}");
				}
			}

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
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}
}
