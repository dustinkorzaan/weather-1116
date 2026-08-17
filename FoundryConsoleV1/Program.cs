using Azure;
using Azure.AI.OpenAI;
using Core.AIWeather.Models;
using Core.HelloWorld.Handlers;
using Core.Geo.Events;
using Core.Json;
using Core.Weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
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
		services.AddMemoryCache();
		services.AddHttpClient();
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
		 - Model Direct (using legacy AzureOpenAIClient against cognitiveservices endpoint)
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

		var endpoint = new Uri("https://wx1116-prd-res-eu2.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		var messages = new List<ChatMessage>()
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt),
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages);
			Console.WriteLine("\nResponse:");
			Console.WriteLine(response.Value.Content[0].Text);
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
		 - Model Direct (using legacy AzureOpenAIClient against cognitiveservices endpoint)
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

		var endpoint = new Uri("https://wx1116-prd-res-eu2.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		var messages = new List<ChatMessage>()
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt),
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages);
			Console.WriteLine("\nResponse:");
			Console.WriteLine(response.Value.Content[0].Text);
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
		 - Model Direct (using legacy AzureOpenAIClient against cognitiveservices endpoint)
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
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Do not use C, KPH, or MM.
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

		var endpoint = new Uri("https://wx1116-prd-res-eu2.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		var messages = new List<ChatMessage>()
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt),
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages);

			Console.WriteLine("\nResponse:");
			Console.WriteLine(response.Value.Content[0].Text);
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
		 - Model Direct (using legacy AzureOpenAIClient against cognitiveservices endpoint)
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
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Do not use C, KPH, or MM.

		Return valid JSON with these fields:
		- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
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

		var endpoint = new Uri("https://wx1116-prd-res-eu2.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		var messages = new List<ChatMessage>()
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt),
		};

		ChatCompletionOptions options = new()
		{
			ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
				jsonSchemaFormatName: "ai_weather_response",
				jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
				jsonSchemaIsStrict: true)
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages, options);
			var content = response.Value.Content[0].Text;
			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				content,
				JsonDefaults.CaseInsensitive);

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
