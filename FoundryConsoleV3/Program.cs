using Core.AIWeather.Models;
using Core.HelloWorld.handlers;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
	private static async Task Main(string[] args)
	{
		Env.TraversePath().Load();

		var services = new ServiceCollection();
		services.AddLogging(logging => logging.AddConsole());
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());
		using var serviceProvider = services.BuildServiceProvider();
		var mediator = serviceProvider.GetRequiredService<IMediator>();

		string location = "Nashville, TN";

		await GetWeatherJsonInJsonOut(mediator, location);
	}





	private static async Task GetWeatherJsonInJsonOut(IMediator mediator, string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 4
		 - Ask AI "What is the current weather in {location}?"
		 - ResponsesClient with injected function tools (GetLatLongData, GetPublicWeatherData)
		 - Model can call tools to derive lat/long and fetch public weather
		 - JSON output from AI
		""");

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		You can call the GetLatLongData tool to resolve a place name to latitude/longitude,
		and the GetPublicWeatherData tool to fetch current public weather for those coordinates.
		Use those tools whenever you need real weather data.
		""";
		var userPrompt = $"""
		What is the current weather today for {location}?
		Use the available function tools to look up coordinates and public weather data.

		Return valid JSON with these fields:
		- fullSummary (string) (full sentence summary of the current weather including temperature, wind speed, wind direction, and conditions)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirection (string)
		- conditions (string)

		Use {location} as the location context.

		You only return valid JSON.
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

		FunctionTool getLatLongTool = ResponseTool.CreateFunctionTool(
			functionName: "GetLatLongData",
			functionDescription: "Resolve a location name to latitude and longitude using public geocoding data.",
			functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
			{
			  "type": "object",
			  "properties": {
			    "location": {
			      "type": "string",
			      "description": "City and optional region/country, e.g. Nashville, TN"
			    }
			  },
			  "required": ["location"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		FunctionTool getPublicWeatherTool = ResponseTool.CreateFunctionTool(
			functionName: "GetPublicWeatherData",
			functionDescription: "Get current public weather conditions for a latitude and longitude.",
			functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
			{
			  "type": "object",
			  "properties": {
			    "latitude": {
			      "type": "number",
			      "description": "Latitude in decimal degrees"
			    },
			    "longitude": {
			      "type": "number",
			      "description": "Longitude in decimal degrees"
			    }
			  },
			  "required": ["latitude", "longitude"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		List<ResponseItem> inputItems =
		[
			ResponseItem.CreateUserMessageItem(userPrompt),
		];

		try
		{
			bool requiresAction;
			string? finalContent = null;

			do
			{
				requiresAction = false;

				CreateResponseOptions options = new(deploymentName, inputItems)
				{
					Instructions = systemPrompt,
					Tools = { getLatLongTool, getPublicWeatherTool },
					TextOptions = new ResponseTextOptions
					{
						TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
							jsonSchemaFormatName: "ai_weather_response",
							jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
							jsonSchemaIsStrict: true)
					}
				};

				Console.WriteLine("\nCreating response with options...");
				ResponseResult response = await client.CreateResponseAsync(options);

				Console.WriteLine("Adding response output items to input items...");
				inputItems.AddRange(response.OutputItems);

				foreach (ResponseItem outputItem in response.OutputItems)
				{
					if (outputItem is FunctionCallResponseItem functionCall)
					{
						switch (functionCall.FunctionName)
						{
							case "GetLatLongData":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									string toolLocation = argumentsJson.RootElement.GetProperty("location").GetString()
										?? throw new InvalidOperationException("GetLatLongData requires a location argument.");

									Console.WriteLine($"\nTool call: GetLatLongData({toolLocation})");
									var latLong = await mediator.Send(new GetLatLongDataEvent { Location = toolLocation });
									string functionOutput = JsonSerializer.Serialize(latLong, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherData":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

									Console.WriteLine($"\nTool call: GetPublicWeatherData({latitude}, {longitude})");
									var weatherData = await mediator.Send(new GetPublicWeatherDataEvent
									{
										LatLong = new NonAILatLongResponse
										{
											Latitude = latitude,
											Longitude = longitude,
										}
									});
									string functionOutput = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							default:
								throw new NotImplementedException($"Unexpected tool call: {functionCall.FunctionName}");
						}

						requiresAction = true;
					}
				}

				if (!requiresAction)
				{
					finalContent = response.GetOutputText();
				}
			} while (requiresAction);

			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				finalContent ?? throw new InvalidOperationException("Model finished without producing content."),
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
