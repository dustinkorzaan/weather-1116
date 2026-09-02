using Core.AIWeather.Models;
using Core.Json;
using Core.Weather;
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

		var location = "Nashville, TN";

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
		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");
		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
		You have access to MCP tools for location mapping and real-time public meteorology data.

		# Tool Protocol
		1. When given a location, immediately call your coordinates resolution tool. It returns ranked matches (rank 1 is best); select the single best-matching place using name, state, and country — normally rank 1, but you may skip rank 1 when a lower rank is clearly correct.
		2. Use the latitude and longitude from the best result (normally rank 1) to invoke your weather fetching tool. Fetch weather for that location only — do not query multiple matches.
		3. You must query these tools whenever real weather data is required to fulfill the request.

		Return valid JSON with these fields:
		- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirectionSourceDegrees (integer): Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
		- windDirectionSource (string): 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
		- conditions (string)
		- latitude (number): Decimal degrees from the best geo result (positive north, negative south).
		- longitude (number): Decimal degrees from the best geo result (positive east, negative west).

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
		    "windDirectionSourceDegrees": { "type": "integer" },
		    "windDirectionSource": { "type": "string" },
		    "conditions": { "type": "string" },
		    "latitude": { "type": "number" },
		    "longitude": { "type": "number" }
		  },
		  "required": ["fullSummary", "temperatureF", "windSpeedMPH", "windDirectionSourceDegrees", "windDirectionSource", "conditions", "latitude", "longitude"],
		  "additionalProperties": false
		}
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		Console.WriteLine("\nAI Output Schema:");
		Console.WriteLine(aiOutputSchema);


		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var mcpSrvFuncAppKey = Environment.GetEnvironmentVariable("MCP_SRV_FUNC_APP_KEY") ?? throw new InvalidOperationException("MCP_SRV_FUNC_APP_KEY not found in environment variables.");
		var mcpSrvAppServiceKey = Environment.GetEnvironmentVariable("MCP_SRV_APP_SERVICE_KEY") ?? throw new InvalidOperationException("MCP_SRV_APP_SERVICE_KEY not found in environment variables.");

		var myMcpSrvFuncApp = ResponseTool.CreateMcpTool(
			serverLabel: "McpSrvFuncApp",
			serverUri: new Uri("https://weather1116-prod-mcp-srv-func-app-b3a6f0cmhqcya3bw.westus2-01.azurewebsites.net/runtime/webhooks/mcp"),
			headers: new Dictionary<string, string> { ["x-functions-key"] = mcpSrvFuncAppKey },
			toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

		var myMcpSrvAppService = ResponseTool.CreateMcpTool(
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

		var options = new CreateResponseOptions(deploymentName, inputItems)
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
			var response = (await client.CreateResponseAsync(options)).Value;

			var content = response.GetOutputText();
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(content);

			if (aiWeather is null)
			{
				Console.WriteLine("Received empty or invalid JSON response.");
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
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}
}
