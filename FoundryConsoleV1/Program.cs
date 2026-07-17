using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

internal class Program
{
	private static async Task Main(string[] args)
	{
		Env.TraversePath().Load();

		string location = "Nashville, TN";

		await GetWeatherWillFail(location);
		await GetWeatherMakeUpSomething(location);

		await GetWeatherJsonInStringOut(location);

		await GetWeatherJsonInJsonOut(location);
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





	private static async Task GetWeatherJsonInStringOut(string location)
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
		var latLong = await GetLatLongData(location);
		var weatherData = await GetWeatherData(latLong);
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





	private static async Task GetWeatherJsonInJsonOut(string location)
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
		var latLong = await GetLatLongData(location);
		var weatherData = await GetWeatherData(latLong);
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





	private static async Task<NonAIWeatherResponse> GetWeatherData(NonAILatLongResponse latLong)
	{
		var client = new HttpClient();
		var currentWeatherPath = "forecast";

		string url = $"https://api.open-meteo.com/v1/{currentWeatherPath}?latitude={latLong.Latitude}&longitude={latLong.Longitude}&current_weather=true";
		Console.WriteLine($"Non-AI: Fetching weather data from: {url}");

		try
		{
			// 1. Fetch raw JSON string from API
			string jsonResponse = await client.GetStringAsync(url);

			// 2. Options to format the console output nicely
			var options = new JsonSerializerOptions { WriteIndented = true };

			// 3. Deserialize into the C# Class Model
			NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse, options) ?? new NonAIWeatherResponse();

			// 4. Return deserialized weather data
			return weatherData;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
			return new NonAIWeatherResponse();
		}
	}

	private static async Task<NonAILatLongResponse> GetLatLongData(string location)
	{
		var client = new HttpClient();

		try
		{
			// Try multiple location variants to handle inputs like "City, ST".
			var queries = new List<string> { location };
			if (location.Contains(','))
			{
				queries.Add(location.Split(',')[0].Trim());
			}

			foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				string encodedLocation = Uri.EscapeDataString(query);
				string url = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedLocation}&count=1&language=en&format=json";
				Console.WriteLine($"Non-AI: Fetching geocoding data from: {url}");
				string jsonResponse = await client.GetStringAsync(url);
				var geoData = JsonSerializer.Deserialize<NonAIGeocodingResponse>(jsonResponse);

				if (geoData?.Results != null && geoData.Results.Count > 0)
				{
					var topMatch = geoData.Results[0];
					Console.WriteLine($"Non-AI: Found: {topMatch.Name}, {topMatch.Admin1}, {topMatch.Country}");
					return new NonAILatLongResponse { Latitude = topMatch.Latitude, Longitude = topMatch.Longitude };
				}
			}

			Console.WriteLine($"Non-AI: No results found for '{location}'.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Non-AI: An error occurred: {ex.Message}");
		}

		// BNA: 36.1317° N, -86.6688° W
		return new NonAILatLongResponse { Latitude = 36.1317, Longitude = -86.6688 };
	}
}





public class NonAIGeocodingResponse
{
	[JsonPropertyName("results")]
	public List<NonAIGeocodingResult> Results { get; set; } = [];
}

public class NonAIGeocodingResult
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("admin1")]
	public string Admin1 { get; set; } = string.Empty;

	[JsonPropertyName("country")]
	public string Country { get; set; } = string.Empty;

	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }
}

public class NonAILatLongResponse
{
	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }
}

public class NonAIWeatherResponse
{
	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }

	[JsonPropertyName("generationtime_ms")]
	public double GenerationTimeMs { get; set; }

	[JsonPropertyName("utc_offset_seconds")]
	public int UtcOffsetSeconds { get; set; }

	[JsonPropertyName("timezone")]
	public string Timezone { get; set; } = string.Empty;

	[JsonPropertyName("timezone_abbreviation")]
	public string TimezoneAbbreviation { get; set; } = string.Empty;

	[JsonPropertyName("elevation")]
	public double Elevation { get; set; }

	[JsonPropertyName("current_weather_units")]
	public NonAICurrentWeatherUnits CurrentWeatherUnits { get; set; } = new();

	[JsonPropertyName("current_weather")]
	public NonAICurrentWeather CurrentWeather { get; set; } = new();
}

public class NonAICurrentWeatherUnits
{
	[JsonPropertyName("time")]
	public string Time { get; set; } = string.Empty;

	[JsonPropertyName("interval")]
	public string Interval { get; set; } = string.Empty;

	[JsonPropertyName("temperature")]
	public string Temperature { get; set; } = string.Empty;

	[JsonPropertyName("windspeed")]
	public string WindSpeed { get; set; } = string.Empty;

	[JsonPropertyName("winddirection")]
	public string WindDirection { get; set; } = string.Empty;

	[JsonPropertyName("is_day")]
	public string IsDay { get; set; } = string.Empty;

	[JsonPropertyName("weathercode")]
	public string WeatherCode { get; set; } = string.Empty;
}

public class NonAICurrentWeather
{
	[JsonPropertyName("time")]
	public string Time { get; set; } = string.Empty;

	[JsonPropertyName("interval")]
	public int Interval { get; set; }

	[JsonPropertyName("temperature")]
	public double Temperature { get; set; }

	[JsonPropertyName("windspeed")]
	public double WindSpeed { get; set; }

	[JsonPropertyName("winddirection")]
	public int WindDirection { get; set; }

	[JsonPropertyName("is_day")]
	public int IsDay { get; set; }

	[JsonPropertyName("weathercode")]
	public int WeatherCode { get; set; }
}
