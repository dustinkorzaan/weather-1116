using System.Text;
using OpenAI.Responses;

namespace Core.Chat.Services;

public static class ChatToolDefinitions
{
    public static FunctionTool CreateGetLatLongTool() => ResponseTool.CreateFunctionTool(
        functionName: "GetLatLongData",
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

    public static FunctionTool CreateGetLocationDataTool() => ResponseTool.CreateFunctionTool(
        functionName: "GetLocationData",
        functionDescription: "Turn a latitude and longitude into a simple place label. US results are City, State; elsewhere City, State, Country.",
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
}
