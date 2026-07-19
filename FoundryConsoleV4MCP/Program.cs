using Azure;
using Azure.AI.OpenAI;
using Core.demo.handlers;
using Core.geo.Events;
using Core.weather.Events;
using DotNetEnv;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using System;
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
