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
        functionDescription: "Find the largest cities (by population) within a radius of a latitude and longitude. Returns each city's name, region, country, coordinates, distance in km, and population, largest first. distanceKM defaults to 161 (range 1-1000) and size defaults to 25 (range 0-100); out-of-range values are adjusted, not rejected.",
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
            "distanceKM": {
              "type": ["number", "null"],
              "description": "Search radius in kilometers (1-1000). Null uses the default of 161."
            },
            "size": {
              "type": ["integer", "null"],
              "description": "Maximum number of cities to return (0-100). Null uses the default of 25."
            }
          },
          "required": ["latitude", "longitude", "distanceKM", "size"],
          "additionalProperties": false
        }
        """)),
        strictModeEnabled: true);

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

    public static FunctionTool CreateAddUserPinTool() => ResponseTool.CreateFunctionTool(
        functionName: "AddUserPin",
        functionDescription: "Add a new pin to the user's saved locations map. Use this when the user wants to save a location.",
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

    public static FunctionTool CreateDeleteUserPinTool() => ResponseTool.CreateFunctionTool(
        functionName: "DeleteUserPin",
        functionDescription: "Remove a pin from the user's saved locations map. Use this when the user wants to delete a saved location.",
        functionParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes("""
        {
          "type": "object",
          "properties": {
            "userPinId": {
              "type": "string",
              "description": "The unique identifier (GUID) of the pin to delete"
            }
          },
          "required": ["userPinId"],
          "additionalProperties": false
        }
        """)),
        strictModeEnabled: true);
}
