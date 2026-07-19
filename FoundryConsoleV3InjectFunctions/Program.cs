using Core.demo.handlers;
using Core.geo.Events;
using Core.geo.Models;
using Core.weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

		await GetWeatherWillFail(location);
		await GetWeatherMakeUpSomething(location);

		await GetWeatherJsonInStringOut(mediator, location);

		await GetWeatherJsonInJsonOut(mediator, location);
	}




	private static async Task GetWeatherWillFail(string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 1
		 - Ask AI "What is the current weather in {location}?"
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - This is expected to fail because it doesn't have supporting data.
		""");

		// AI prep
		var systemPrompt = "You are a helpful weather assistant.";
		var userPrompt = $"""
		What is the current weather today for {location}?
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		CreateResponseOptions options = new()
		{
			Model = deploymentName,
			Instructions = systemPrompt,
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		try
		{
			ResponseResult response = await client.CreateResponseAsync(options);
			Console.WriteLine("\nResponse:");
			Console.WriteLine(response.GetOutputText());
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}





	private static async Task GetWeatherMakeUpSomething(string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 2
		 - Ask AI "What is the current weather in {location}?"
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - Ask it to make something up because it doesn't have supporting data.
		""");

		// AI prep
		var systemPrompt = "You are a helpful weather assistant.";
		var userPrompt = $"""
		What is the current weather today for {location}?
		- I know you don't have supporting data, so just make something up.
		- Keep it short.
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		CreateResponseOptions options = new()
		{
			Model = deploymentName,
			Instructions = systemPrompt,
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
		};

		try
		{
			ResponseResult response = await client.CreateResponseAsync(options);
			Console.WriteLine("\nResponse:");
			Console.WriteLine(response.GetOutputText());
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
	}





	private static async Task GetWeatherJsonInStringOut(IMediator mediator, string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 3
		 - Ask AI "What is the current weather in {location}?"
		 - ResponsesClient with injected function tools (GetLatLongData, GetPublicWeatherData)
		 - Model can call tools to derive lat/long and fetch public weather
		 - String output from AI
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
		Use the available function tools to look up coordinates and public weather data, then describe the weather.
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
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

			do
			{
				requiresAction = false;

				CreateResponseOptions options = new(deploymentName, inputItems)
				{
					Instructions = systemPrompt,
					Tools = { getLatLongTool, getPublicWeatherTool },
				};

				ResponseResult response = await client.CreateResponseAsync(options);

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
									Console.WriteLine(functionOutput);
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
									Console.WriteLine(functionOutput);
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
					Console.WriteLine("\nResponse:");
					Console.WriteLine(response.GetOutputText());
				}
			} while (requiresAction);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("\nPress any key to continue.");
		Console.ReadKey(true);
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
		- summary (string) (full sentence summary of the current weather including temperature, wind speed, wind direction, and conditions)
		- temperature (number)
		- windSpeed (number)
		- windDirection (string)
		- conditions (string)

		Use {location} as the location context.

		You only return valid JSON.
		""";

		var aiOutputSchema = """
		{
		  "type": "object",
		  "properties": {
		    "summary": { "type": "string" },
		    "temperature": { "type": "number" },
		    "windSpeed": { "type": "number" },
		    "windDirection": { "type": "string" },
		    "conditions": { "type": "string" }
		  },
		  "required": ["summary", "temperature", "windSpeed", "windDirection", "conditions"],
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
			options: new ResponsesClientOptions()
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

				ResponseResult response = await client.CreateResponseAsync(options);

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
									Console.WriteLine(functionOutput);
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
									Console.WriteLine(functionOutput);
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





	public class AIWeatherResponse
	{
		[JsonPropertyName("summary")]
		public string Summary { get; set; } = string.Empty;

		[JsonPropertyName("temperature")]
		public double Temperature { get; set; }

		[JsonPropertyName("windSpeed")]
		public double WindSpeed { get; set; }

		[JsonPropertyName("windDirection")]
		public string WindDirection { get; set; } = string.Empty;

		[JsonPropertyName("conditions")]
		public string Conditions { get; set; } = string.Empty;
	}
}
