using Core;
using Core.AIWeather.Models;
using Core.Geo.Events;
using Core.Json;
using Core.Weather;
using Core.Weather.Events;
using DotNetEnv;
using CQMediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
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
		services.AddStandardCoreServices();
		using var serviceProvider = services.BuildServiceProvider();
		var mediator = serviceProvider.GetRequiredService<IMediator>();

		var location = "Nashville, TN";

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

		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		const string deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var options = new CreateResponseOptions()
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
			var response = (await client.CreateResponseAsync(options)).Value;
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
		var systemPrompt = """
		You are a helpful weather assistant.
		- I know you don't have supporting data, so just make something up.
		- Keep it short.
		""";
		var userPrompt = $"""
		What is the current weather today for {location}?
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		const string deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var options = new CreateResponseOptions()
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
			var response = (await client.CreateResponseAsync(options)).Value;
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
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - Provide raw JSON input from a weather API
		 - String output from AI
		""");

		// Non-AI prep
		var latLongMatches = await mediator.Send(new GetLatLongEvent { Location = location, Count = 1 });
		var latLong = latLongMatches.Results[0];
		var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
		{
			Latitude = latLong.Latitude,
			Longitude = latLong.Longitude,
		});
		var weatherDataJson = JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
		GitHub-flavored Markdown is allowed when it makes the answer easier to read. Do not emit raw HTML.
		Use one or two friendly sentences of the current weather and include the place name, temperature, wind speed, wind direction, and overall conditions. Keep those facts even if they also appear in JSON.
		""";
		var userPrompt = $"""
		You are given this WeatherConditions JSON:
		{weatherDataJson}

		Describe today's current weather in {location}?
		""";

		Console.WriteLine("\nSystem Prompt:");
		Console.WriteLine(systemPrompt);

		Console.WriteLine("\nUser Prompt:");
		Console.WriteLine(userPrompt);

		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		const string deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var options = new CreateResponseOptions()
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
			var response = (await client.CreateResponseAsync(options)).Value;
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





	private static async Task GetWeatherJsonInJsonOut(IMediator mediator, string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 4
		 - Ask AI "What is the current weather in {location}?"
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - Provide raw JSON input from a weather API
		 - JSON output from AI
		""");

		// Non-AI prep
		var latLongMatches = await mediator.Send(new GetLatLongEvent { Location = location, Count = 1 });
		var latLong = latLongMatches.Results[0];
		var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
		{
			Latitude = latLong.Latitude,
			Longitude = latLong.Longitude,
		});
		var weatherDataJson = JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.

		Return valid JSON with these fields:
		- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirectionSourceDegrees (integer): Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
		- windDirectionSource (string): 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
		- conditions (string)
		- latitude (number): Decimal degrees from the provided WeatherConditions JSON (positive north, negative south).
		- longitude (number): Decimal degrees from the provided WeatherConditions JSON (positive east, negative west).

		You only return valid JSON.
		""";
		var userPrompt = $"""
		You are given this WeatherConditions JSON:
		{weatherDataJson}

		Use {location} as the location context.
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

		var endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		const string deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions()
			{
				Endpoint = new Uri(endpoint),
			});

		var options = new CreateResponseOptions()
		{
			Model = deploymentName,
			Instructions = systemPrompt,
			InputItems =
			{
				ResponseItem.CreateUserMessageItem(userPrompt),
			},
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
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				content,
				JsonDefaults.CaseInsensitive);

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
