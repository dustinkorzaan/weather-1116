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

    public static FunctionTool CreateGetPublicWeatherTool() => ResponseTool.CreateFunctionTool(
        functionName: "GetPublicWeatherData",
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
}
