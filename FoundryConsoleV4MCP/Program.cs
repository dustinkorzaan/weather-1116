using Core.demo.handlers;
using Core.geo.Events;
using Core.weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System;
using System.ClientModel;
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
 
		await GetWeatherJsonInJsonOut(mediator, location);
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
		var latLong = await mediator.Send(new GetLatLongDataEvent { Location = location });
		var weatherData = await mediator.Send(new GetPublicWeatherDataEvent { LatLong = latLong });
		var weatherDataJson = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		You provide weather and climate data using U.S. customary units (Fahrenheit and MPH).
		""";
		var userPrompt = $"""
		You are given this WeatherConditions JSON:
		{weatherDataJson}

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
