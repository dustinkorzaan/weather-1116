using Core;
using Core.AIWeather.Models;
using Core.Data;
using Core.Geo.Events;
using Core.Json;
using Core.Users.Events;
using Core.Weather;
using Core.Weather.Events;
using DotNetEnv;
using CQMediator;
using Microsoft.EntityFrameworkCore;
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
		services.AddStandardCoreServices();

		// GetUser/AddUserPin/DeleteUserPin read and write dbo.User/dbo.UserPin. The API owns EF Core
		// migrations; this console only reads/writes the already-migrated schema. Without
		// DB_CONNECTION_STRING the placeholder connection makes those three tool calls fail, while the
		// geo and weather tools still work.
		var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
			Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"),
			Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"));
		services.AddDbContext<WX1116DbContext>(options =>
			options.UseSqlServer(dbConnectionString ?? "Server=(local);"));

		using var serviceProvider = services.BuildServiceProvider();
		var mediator = serviceProvider.GetRequiredService<IMediator>();

		var location = "Nashville, TN";

		await GetWeatherJsonInJsonOut(mediator, location);
	}





	private static async Task GetWeatherJsonInJsonOut(IMediator mediator, string location)
	{
		Console.Clear();
		Console.WriteLine($"""
		Example 4
		 - Ask AI "What is the current weather in {location}?"
		 - ResponsesClient with in-process tool callbacks (GetLatLong, GetLocation, GetCities, GetPublicWeatherCurrent, GetPublicWeatherForecast, GetPublicWeatherHistory, GetUser, AddUserPin, DeleteUserPin)
		 - Model can call tools to derive lat/long, label a coordinate, and fetch public weather
		 - JSON output from AI
		""");

		var endpoint = "https://wx1116prod2th7yydhws5h6.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1";
		var deploymentName = "gpt-5.4-mini";
		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY") ?? throw new InvalidOperationException("API key not found in environment variables.");

		// LM Studio Bionic Demo (3–5 minutes of startup, followed by 3–5 minutes of interactive use over 3–5 loops
		// endpoint = "http://localhost:1234/v1";
		// deploymentName = "qwen/qwen3.5-9b";
		// apiKey = "ollama";

		// AI prep
		var systemPrompt = """
		You are a helpful weather assistant.
		Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
		You can call the GetLatLong tool to resolve a place name to ranked latitude/longitude
		matches (up to 5; rank 1 is the best match). Call GetLocation to turn latitude/longitude into
		a City, State label (City, State, Country outside the US), then a feature name, then a
		formatted coordinate such as 35.51° N, 86.58° W. Call GetCities to list the largest cities
		within a radius of a latitude/longitude. Call GetPublicWeatherCurrent
		for conditions now, GetPublicWeatherForecast for upcoming weather, or GetPublicWeatherHistory
		for the recent past.

		# Tool Protocol
		1. When given a location, immediately call your coordinates resolution tool. It returns ranked matches (rank 1 is best); select the single best-matching place using name, state, and country — normally rank 1, but you may skip rank 1 when a lower rank is clearly correct.
		2. Use the latitude and longitude from the best result (normally rank 1) to invoke your weather fetching tool. Fetch weather for that location only — do not query multiple matches.
		3. You must query these tools whenever real weather data is required to fulfill the request.

		Return valid JSON with these fields:
		- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
		- temperatureF (number) in Fahrenheit
		- windSpeedMPH (number) in MPH
		- windDirectionSourceDegrees (integer): Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
		- windDirectionSource (string): 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
		- conditions (string)
		- latitude (number): Decimal degrees from the best geo result (positive north, negative south).
		- longitude (number): Decimal degrees from the best geo result (positive east, negative west).

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

		var client = new ResponsesClient(
			credential: new ApiKeyCredential(apiKey),
			options: new ResponsesClientOptions
			{
				Endpoint = new Uri(endpoint),
			});

		var getLatLongTool = ResponseTool.CreateFunctionTool(
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

		var getLocationTool = ResponseTool.CreateFunctionTool(
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

		var getCitiesTool = ResponseTool.CreateFunctionTool(
			functionName: "GetCities",
			functionDescription: "Find the largest cities (by population) within a radius of a latitude and longitude. Returns each city's name, region, country, coordinates, distance in km, and population, largest first. radiusKm defaults to 161 (range 1-1000), minPopulation to 0, and maxCities to 25 (range 0-100); out-of-range values are adjusted, not rejected. The search radius is capped at 100 km (the GeoDB free-tier limit), and the result reports the radius actually used.",
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
			    "radiusKm": {
			      "type": ["number", "null"],
			      "description": "Search radius in kilometers (1-1000, default 161). Searches are capped at 100 km, the GeoDB free-tier limit. Null uses the default."
			    },
			    "minPopulation": {
			      "type": ["integer", "null"],
			      "description": "Only include cities with at least this many people (0 or more, default 0). Null uses the default."
			    },
			    "maxCities": {
			      "type": ["integer", "null"],
			      "description": "Maximum number of cities to return (0-100, default 25). Null uses the default."
			    }
			  },
			  "required": ["latitude", "longitude", "radiusKm", "minPopulation", "maxCities"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		var getPublicWeatherCurrentTool = ResponseTool.CreateFunctionTool(
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

		var getPublicWeatherForecastTool = ResponseTool.CreateFunctionTool(
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

		var getPublicWeatherHistoryTool = ResponseTool.CreateFunctionTool(
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

		var getUserTool = ResponseTool.CreateFunctionTool(
			functionName: "GetUser",
			functionDescription: "Get the current user and their saved map pins. Each pin has an id (GUID), locationName, latitude, and longitude. Call this to see which locations the user has saved, and to find a pin's id before calling DeleteUserPin.",
			functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
			{
			  "type": "object",
			  "properties": {},
			  "required": [],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		var addUserPinTool = ResponseTool.CreateFunctionTool(
			functionName: "AddUserPin",
			functionDescription: "Add a new pin to the user's saved locations map. Use this when the user wants to save a location. Requires numeric latitude/longitude and a location name.",
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
			    "locationName": {
			      "type": "string",
			      "description": "Name of the location, e.g. Nashville, Tennessee"
			    }
			  },
			  "required": ["latitude", "longitude", "locationName"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		var deleteUserPinTool = ResponseTool.CreateFunctionTool(
			functionName: "DeleteUserPin",
			functionDescription: "Remove a pin from the user's saved locations map. Use this when the user wants to delete a saved location. Requires the pin's id from GetUser; never guess an id.",
			functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
			{
			  "type": "object",
			  "properties": {
			    "userPinId": {
			      "type": "string",
			      "description": "The unique identifier (GUID) of the pin to delete, from GetUser"
			    }
			  },
			  "required": ["userPinId"],
			  "additionalProperties": false
			}
			""")),
			strictModeEnabled: true);

		var inputItems = new List<ResponseItem>
		{
			ResponseItem.CreateUserMessageItem(userPrompt),
		};

		try
		{
			var requiresAnotherLoop = false;
			string? finalContent = null;

			do
			{
				requiresAnotherLoop = false;

				var options = new CreateResponseOptions(deploymentName, inputItems)
				{
					Instructions = systemPrompt,
					Tools = { getLatLongTool, getLocationTool, getCitiesTool, getPublicWeatherCurrentTool, getPublicWeatherForecastTool, getPublicWeatherHistoryTool, getUserTool, addUserPinTool, deleteUserPinTool },
					TextOptions = new ResponseTextOptions
					{
						TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
							jsonSchemaFormatName: "ai_weather_response",
							jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
							jsonSchemaIsStrict: true)
					}
				};

				Console.WriteLine("\nCreating response with options...");
				var response = (await client.CreateResponseAsync(options)).Value;

				Console.WriteLine("Adding response output items to input items...");
				inputItems.AddRange(response.OutputItems);

				foreach (var outputItem in response.OutputItems)
				{
					if (outputItem is FunctionCallResponseItem functionCall)
					{
						switch (functionCall.FunctionName)
						{
							case "GetLatLong":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var toolLocation = argumentsJson.RootElement.GetProperty("location").GetString()
										?? throw new InvalidOperationException("GetLatLong requires a location argument.");

									Console.WriteLine($"\nTool call: GetLatLong({toolLocation})");
									var latLongMatches = await mediator.Send(new GetLatLongEvent { Location = toolLocation });
									var functionOutput = JsonSerializer.Serialize(latLongMatches, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetLocation":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									var longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

									Console.WriteLine($"\nTool call: GetLocation({latitude}, {longitude})");
									var locationData = await mediator.Send(new GetLocationEvent
									{
										Latitude = latitude,
										Longitude = longitude,
									});
									var functionOutput = JsonSerializer.Serialize(locationData, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetCities":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var root = argumentsJson.RootElement;
									var citiesEvent = new GetCitiesEvent
									{
										Latitude = root.GetProperty("latitude").GetDouble(),
										Longitude = root.GetProperty("longitude").GetDouble(),
									};
									if (root.TryGetProperty("radiusKm", out var radiusElement) && radiusElement.ValueKind == JsonValueKind.Number)
									{
										citiesEvent.RadiusKm = radiusElement.GetDouble();
									}
									if (root.TryGetProperty("minPopulation", out var populationElement) && populationElement.ValueKind == JsonValueKind.Number && populationElement.TryGetInt64(out var minPopulation))
									{
										citiesEvent.MinPopulation = minPopulation;
									}
									if (root.TryGetProperty("maxCities", out var maxCitiesElement) && maxCitiesElement.ValueKind == JsonValueKind.Number && maxCitiesElement.TryGetInt32(out var maxCities))
									{
										citiesEvent.MaxCities = maxCities;
									}

									Console.WriteLine($"\nTool call: GetCities({citiesEvent.Latitude}, {citiesEvent.Longitude}, {citiesEvent.RadiusKm}, {citiesEvent.MinPopulation}, {citiesEvent.MaxCities})");
									var citiesData = await mediator.Send(citiesEvent);
									var functionOutput = JsonSerializer.Serialize(citiesData, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherCurrent":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									var longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

									Console.WriteLine($"\nTool call: GetPublicWeatherCurrent({latitude}, {longitude})");
									var weatherData = await mediator.Send(new GetPublicWeatherCurrentEvent
									{
										Latitude = latitude,
										Longitude = longitude,
									});
									var functionOutput = JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherForecast":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									var longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
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
									var functionOutput = JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetPublicWeatherHistory":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									var longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
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
									var functionOutput = JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "GetUser":
								{
									Console.WriteLine("\nTool call: GetUser()");
									var user = await mediator.Send(new GetUserEvent());
									var functionOutput = JsonSerializer.Serialize(user, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "AddUserPin":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
									var longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
									var locationName = argumentsJson.RootElement.GetProperty("locationName").GetString()
										?? throw new InvalidOperationException("AddUserPin requires a locationName argument.");

									Console.WriteLine($"\nTool call: AddUserPin({latitude}, {longitude}, {locationName})");
									await mediator.Send(new AddUserPinEvent
									{
										Latitude = latitude,
										Longitude = longitude,
										LocationName = locationName,
									});
									var functionOutput = JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							case "DeleteUserPin":
								{
									using var argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
									var userPinId = argumentsJson.RootElement.GetProperty("userPinId").GetString();
									if (!Guid.TryParse(userPinId, out var pinId))
									{
										throw new InvalidOperationException("userPinId must be a valid GUID.");
									}

									Console.WriteLine($"\nTool call: DeleteUserPin({pinId})");
									await mediator.Send(new DeleteUserPinEvent { UserPinId = pinId });
									var functionOutput = JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
									Console.WriteLine($"Tool output: {functionOutput}");
									inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
									break;
								}

							default:
								throw new NotImplementedException($"Unexpected tool call: {functionCall.FunctionName}");
						}

						requiresAnotherLoop = true;
					}
				}

				if (!requiresAnotherLoop)
				{
					finalContent = response.GetOutputText();
				}
			} while (requiresAnotherLoop);

			var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
				finalContent ?? throw new InvalidOperationException("Model finished without producing content."));

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
