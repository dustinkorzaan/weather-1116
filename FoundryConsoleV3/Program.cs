using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Models;
using Core.HelloWorld.Handlers;
using Core.Geo.Events;
using Core.Weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
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
		 - ResponsesClient with in-process tool callbacks (GetLatLong, GetLocation, GetPublicWeatherCurrent, GetPublicWeatherForecast, GetPublicWeatherHistory)
		 - Model can call tools to derive lat/long, label a coordinate, and fetch public weather
		 - JSON output from AI
		""");

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Do not use C, KPH, or MM.
		You can call the GetLatLong tool to resolve a place name to ranked latitude/longitude
		matches (up to 5; rank 1 is the best match). Use name, state, and country to pick the
		right place — you may skip rank 1. Call GetLocation to turn latitude/longitude into
		a City, State label (City, State, Country outside the US), then a feature name, then a
		formatted coordinate such as 35.51° N, 86.58° W. Then call GetPublicWeatherCurrent
		for conditions now, GetPublicWeatherForecast for upcoming weather, or GetPublicWeatherHistory
		for the recent past.
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
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ProjectOpenAIClient projectOpenAIClient = new(
			ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
			new ProjectOpenAIClientOptions
			{
				Endpoint = new Uri(endpoint),
			});

		ProjectResponsesClient client = projectOpenAIClient.GetProjectResponsesClientForModel(deploymentName);

		FunctionTool getLatLongTool = ResponseTool.CreateFunctionTool(
			functionName: "GetLatLong",
			functionDescription: "Resolve a location name to ranked latitude/longitude matches using public geocoding data. Returns up to 5 results (rank 1 is the best match). Use state and country to pick the right place if rank 1 is wrong.",
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

		FunctionTool getLocationTool = ResponseTool.CreateFunctionTool(
			functionName: "GetLocation",
			functionDescription: "Turn a latitude and longitude into a simple place label. Prefers City, State in the US (City, State, Country elsewhere), then a feature name, then a formatted coordinate such as 35.51° N, 86.58° W.",
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

		FunctionTool getPublicWeatherCurrentTool = ResponseTool.CreateFunctionTool(
			functionName: "GetPublicWeatherCurrent",
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

		FunctionTool getPublicWeatherForecastTool = ResponseTool.CreateFunctionTool(
			functionName: "GetPublicWeatherForecast",
			functionDescription: "Get an upcoming public weather forecast for a latitude and longitude. Daily is the next 7 days, Hourly is the next 48 hours, and FifteenMinutes is the next 48 hours in 15-minute steps. Use Daily unless the user asks for hourly or 15-minute detail.",
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
			    },
			    "resolution": {
			      "type": "string",
			      "enum": ["Daily", "Hourly", "FifteenMinutes"],
			      "description": "Daily (next 7 days), Hourly (next 48 hours), or FifteenMinutes (next 48 hours). Defaults to Daily."
			    }
			  },
			  "required": ["latitude", "longitude", "resolution"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		FunctionTool getPublicWeatherHistoryTool = ResponseTool.CreateFunctionTool(
			functionName: "GetPublicWeatherHistory",
			functionDescription: "Get recent past public weather for a latitude and longitude. Daily is the previous 7 days, Hourly is the previous 48 hours. Use Daily unless the user asks for hourly detail.",
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
			    },
			    "resolution": {
			      "type": "string",
			      "enum": ["Daily", "Hourly"],
			      "description": "Daily (previous 7 days) or Hourly (previous 48 hours). Defaults to Daily."
			    }
			  },
			  "required": ["latitude", "longitude", "resolution"],
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
					Tools = { getLatLongTool, getLocationTool, getPublicWeatherCurrentTool, getPublicWeatherForecastTool, getPublicWeatherHistoryTool },
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
							case "GetLatLong":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									string toolLocation = argumentsJson.RootElement.GetProperty("location").GetString()
										?? throw new InvalidOperationException("GetLatLong requires a location argument.");

									Console.WriteLine($"\nTool call: GetLatLong({toolLocation})");
									var latLongMatches = await mediator.Send(new GetLatLongEvent { Location = toolLocation });
									string functionOutput = JsonSerializer.Serialize(latLongMatches, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetLocation":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

									Console.WriteLine($"\nTool call: GetLocation({latitude}, {longitude})");
									var locationData = await mediator.Send(new GetLocationEvent
									{
										Latitude = latitude,
										Longitude = longitude,
									});
									string functionOutput = JsonSerializer.Serialize(locationData, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherCurrent":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

									Console.WriteLine($"\nTool call: GetPublicWeatherCurrent({latitude}, {longitude})");
									var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
									{
										Latitude = latitude,
										Longitude = longitude,
									});
									string functionOutput = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherForecast":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
									var resolution = PublicWeatherForecastResolution.Daily;
									if (argumentsJson.RootElement.TryGetProperty("resolution", out var resolutionElement)
										&& resolutionElement.GetString() is string resolutionText
										&& Enum.TryParse(resolutionText, ignoreCase: true, out PublicWeatherForecastResolution parsedResolution))
									{
										resolution = parsedResolution;
									}

									Console.WriteLine($"\nTool call: GetPublicWeatherForecast({latitude}, {longitude}, {resolution})");
									var weatherData = await mediator.Send(new GetPublicWeatherForecastEvent
									{
										Latitude = latitude,
										Longitude = longitude,
										Resolution = resolution,
									});
									string functionOutput = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherHistory":
								{
									using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
									var resolution = PublicWeatherHistoryResolution.Daily;
									if (argumentsJson.RootElement.TryGetProperty("resolution", out var resolutionElement)
										&& resolutionElement.GetString() is string resolutionText
										&& Enum.TryParse(resolutionText, ignoreCase: true, out PublicWeatherHistoryResolution parsedResolution))
									{
										resolution = parsedResolution;
									}

									Console.WriteLine($"\nTool call: GetPublicWeatherHistory({latitude}, {longitude}, {resolution})");
									var weatherData = await mediator.Send(new GetPublicWeatherHistoryEvent
									{
										Latitude = latitude,
										Longitude = longitude,
										Resolution = resolution,
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
				finalContent ?? throw new InvalidOperationException("Model finished without producing content."));

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
