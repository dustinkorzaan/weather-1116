using System.Text;
using OpenAI.Responses;

namespace Core.Tools;

public static class WeatherToolDefinitions
{
    public static FunctionTool CreateGetLatLongTool() => ResponseTool.CreateFunctionTool(
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

    public static FunctionTool CreateGetLocationTool() => ResponseTool.CreateFunctionTool(
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

    public static FunctionTool CreateGetCitiesTool() => ResponseTool.CreateFunctionTool(
        functionName: "GetCities",
        functionDescription: GetCitiesDescription,
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
              "description": "Search radius in kilometers (1-1000, default 161). Null uses the default."
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

    public const string GetCitiesDescription =
        "Find the largest cities (by population) within a radius of a latitude and longitude. Returns each city's name, region, country, coordinates, distance in km, and population, largest first. radiusKm defaults to 161 (range 1-1000), minPopulation to 0, and maxCities to 25 (range 0-100); out-of-range values are adjusted, not rejected. The result reports the radius actually used; country is the two-letter ISO country code (e.g. US).";

    public static FunctionTool CreateGetPublicWeatherCurrentTool() => ResponseTool.CreateFunctionTool(
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

    public static FunctionTool CreateGetPublicWeatherForecastTool() => ResponseTool.CreateFunctionTool(
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

    public static FunctionTool CreateGetPublicWeatherHistoryTool() => ResponseTool.CreateFunctionTool(
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

    public static FunctionTool CreateGetUserTool() => ResponseTool.CreateFunctionTool(
        functionName: "GetUser",
        functionDescription: GetUserDescription,
        functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
        {
          "type": "object",
          "properties": {},
          "required": [],
          "additionalProperties": false
        }
        """)),
        strictModeEnabled: true);

    public const string GetUserDescription =
        "Get the current user and their saved cities. Each saved city has an id (GUID), locationName, latitude, and longitude. Call this to see which locations the user has saved, and to find a saved city's id before calling DeleteUserCity.";

    public static FunctionTool CreateAddUserCityTool() => ResponseTool.CreateFunctionTool(
        functionName: "AddUserCity",
        functionDescription: AddUserCityDescription,
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

    public static FunctionTool CreateDeleteUserCityTool() => ResponseTool.CreateFunctionTool(
        functionName: "DeleteUserCity",
        functionDescription: DeleteUserCityDescription,
        functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
        {
          "type": "object",
          "properties": {
            "userCityId": {
              "type": "string",
              "description": "The unique identifier (GUID) of the saved city to delete, from GetUser"
            }
          },
          "required": ["userCityId"],
          "additionalProperties": false
        }
        """)),
        strictModeEnabled: true);

    public const string AddUserCityDescription =
        "Add a city to the user's saved cities. Use this when the user wants to save a city or place. Requires numeric latitude/longitude and a location name.";

    public const string DeleteUserCityDescription =
        "Remove a city from the user's saved cities. Use this when the user wants to delete a saved city. Requires the saved city's id from GetUser; never guess an id.";
}
