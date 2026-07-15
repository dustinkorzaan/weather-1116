using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program
{
	private static async Task Main(string[] args)
	{
		string location = "Nashville, TN";

		await WhatIsTodaysCurrentWeather(location);
		await WhatIsTodaysCurrentWeatherJsonIn(location);
		await WhatIsTodaysCurrentWeatherJsonInJsonOut(location);
	}

	private static async Task WhatIsTodaysCurrentWeather(string location)
	{
		Console.Clear();
		Console.WriteLine("Method 1 - What is today's current weather?");

		var endpoint = new Uri("https://foundrydemo1116.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = "";

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		List<ChatMessage> messages = new List<ChatMessage>()
		{
			new SystemChatMessage("You are a helpful weather assistant."),
			new UserChatMessage($"""
			What is today's weather for {location}?
			- I know you don't have supporting data, so just make something up.
			- Keep it short.
			"""),
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages);
			Console.WriteLine(response.Value.Content[0].Text);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("Press any key to continue.");
		Console.ReadKey(true);
		Console.WriteLine();
	}

	private static async Task WhatIsTodaysCurrentWeatherJsonIn(string location)
	{
		Console.Clear();
		Console.WriteLine("Method 2 - What is today's current weather?");
		
		var latLong = await GetLatLongData(location);
		var weatherData = await GetWeatherData(latLong);
		var weatherDataJson = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });

		var prompt = $"""
		You are given these WeatherConditions for {location} in JSON:
		{weatherDataJson}

		Describe today's current weather in {location}?
		Use only Fahrenheit for temperature and only MPH for wind speed.
		""";

		var endpoint = new Uri("https://foundrydemo1116.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = "";

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		List<ChatMessage> messages = new List<ChatMessage>()
		{
			new SystemChatMessage("You are a helpful weather assistant."),
			new UserChatMessage(prompt),
		};

		try
		{
			var response = await chatClient.CompleteChatAsync(messages);
			Console.WriteLine(response.Value.Content[0].Text);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("Press any key to continue.");
		Console.ReadKey(true);
		Console.WriteLine();
	}

	private static async Task WhatIsTodaysCurrentWeatherJsonInJsonOut(string location)
	{
		Console.Clear();
		Console.WriteLine("Method 3 - What is today's current weather? JSON in, JSON out.");

		var latLong = await GetLatLongData(location);
		var weatherData = await GetWeatherData(latLong);
		var weatherDataJson = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
		var aiOutputSchema = """
		{
		  "type": "object",
		  "properties": {
		    "summary": { "type": "string" },
		    "temperature": { "type": "number" }
		  },
		  "required": ["summary", "temperature"],
		  "additionalProperties": false
		}
		""";

		var prompt = $"""
		You are given this WeatherConditions JSON:
		{weatherDataJson}

		Create a concise current weather summary for {location} based only on this data.
		Use only Fahrenheit for temperature and only MPH for wind speed.
		Return valid JSON only.
		""";

		var endpoint = new Uri("https://foundrydemo1116.cognitiveservices.azure.com/");
		var deploymentName = "gpt-5.4-mini";
		var apiKey = "";

		AzureOpenAIClient azureClient = new(
			endpoint,
			new AzureKeyCredential(apiKey));
		ChatClient chatClient = azureClient.GetChatClient(deploymentName);

		List<ChatMessage> messages = new List<ChatMessage>()
		{
			new SystemChatMessage("You are a helpful weather assistant."),
			new UserChatMessage(prompt),
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
				Console.WriteLine(JsonSerializer.Serialize(aiWeather, new JsonSerializerOptions { WriteIndented = true }));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Request failed: {ex.Message}");
		}

		Console.WriteLine("Press any key to continue.");
		Console.ReadKey(true);
		Console.WriteLine();
	}

	private static async Task<WeatherResponse> GetWeatherData(LatLongResponse latLong)
	{
		var client = new HttpClient();
		var currentWeatherPath = "forecast";

		string url = $"https://api.open-meteo.com/v1/{currentWeatherPath}?latitude={latLong.Latitude}&longitude={latLong.Longitude}&current_weather=true";

		try
		{
			// 1. Fetch raw JSON string from API
			string jsonResponse = await client.GetStringAsync(url);

			// 2. Options to format the console output nicely
			var options = new JsonSerializerOptions { WriteIndented = true };

			// 3. Deserialize into the C# Class Model
			WeatherResponse weatherData = JsonSerializer.Deserialize<WeatherResponse>(jsonResponse, options);

			// 4. Serialize back to JSON and console print as-is
			string outputJson = JsonSerializer.Serialize(weatherData, options);
			return weatherData;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
			return new WeatherResponse();
		}
	}

	private static async Task<LatLongResponse> GetLatLongData(string location)
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
				string jsonResponse = await client.GetStringAsync(url);
				var geoData = JsonSerializer.Deserialize<GeocodingResponse>(jsonResponse);

				if (geoData?.Results != null && geoData.Results.Count > 0)
				{
					var topMatch = geoData.Results[0];
				Console.WriteLine($"Found: {topMatch.Name}, {topMatch.Admin1}, {topMatch.Country}");
					return new LatLongResponse { Latitude = topMatch.Latitude, Longitude = topMatch.Longitude };
				}
			}

			Console.WriteLine($"No results found for '{location}'.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
		}

		// BNA: 36.1317° N, -86.6688° W
		return new LatLongResponse { Latitude = 36.1317, Longitude = -86.6688 };
	}
}

public class GeocodingResponse
{
	[JsonPropertyName("results")]
	public List<GeocodingResult> Results { get; set; }
}

public class GeocodingResult
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("admin1")]
	public string Admin1 { get; set; }

	[JsonPropertyName("country")]
	public string Country { get; set; }

	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }
}

public class LatLongResponse
{
	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }
}

public class WeatherResponse
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
	public string Timezone { get; set; }

	[JsonPropertyName("timezone_abbreviation")]
	public string TimezoneAbbreviation { get; set; }

	[JsonPropertyName("elevation")]
	public double Elevation { get; set; }

	[JsonPropertyName("current_weather_units")]
	public CurrentWeatherUnits CurrentWeatherUnits { get; set; }

	[JsonPropertyName("current_weather")]
	public CurrentWeather CurrentWeather { get; set; }
}

public class CurrentWeatherUnits
{
	[JsonPropertyName("time")]
	public string Time { get; set; }

	[JsonPropertyName("interval")]
	public string Interval { get; set; }

	[JsonPropertyName("temperature")]
	public string Temperature { get; set; }

	[JsonPropertyName("windspeed")]
	public string WindSpeed { get; set; }

	[JsonPropertyName("winddirection")]
	public string WindDirection { get; set; }

	[JsonPropertyName("is_day")]
	public string IsDay { get; set; }

	[JsonPropertyName("weathercode")]
	public string WeatherCode { get; set; }
}

public class CurrentWeather
{
	[JsonPropertyName("time")]
	public string Time { get; set; }

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

public class AIWeatherResponse
{
	[JsonPropertyName("summary")]
	public string Summary { get; set; }

	[JsonPropertyName("temperature")]
	public double Temperature { get; set; }
}
