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
			options: new OpenAIClientOptions()
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

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new OpenAIClientOptions()
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
		 - Model Direct (using ResponsesClient against unified AI services endpoint)
		 - Provide raw JSON input from a weather API
		 - String output from AI
		""");

		// Non-AI prep
		var latLongMatches = await mediator.Send(new GetLatLongDataEvent { Location = location, Count = 1 });
		var latLong = latLongMatches.Results[0];
		var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
		{
			Latitude = latLong.Latitude,
			Longitude = latLong.Longitude,
		});
		var weatherDataJson = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		GitHub-flavored Markdown is allowed when it makes the answer easier to read. Do not emit raw HTML.
		Use one or two sentences and include the place name, latitude, longitude, temperature, wind speed, wind direction, and overall conditions. Keep those facts even if they also appear in JSON.
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

		const string deploymentName = "gpt-5.4-mini";
		const string endpoint = "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new OpenAIClientOptions()
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
		var latLongMatches = await mediator.Send(new GetLatLongDataEvent { Location = location, Count = 1 });
		var latLong = latLongMatches.Results[0];
		var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
		{
			Latitude = latLong.Latitude,
			Longitude = latLong.Longitude,
		});
		var weatherDataJson = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).

		Return valid JSON with these fields:
		- fullSummary (string) (one or two sentences including place name, latitude, longitude, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirection (string)
		- conditions (string)

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

		CreateResponseOptions options = new()
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
			ResponseResult response = await client.CreateResponseAsync(options);
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
