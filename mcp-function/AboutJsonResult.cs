using Core.about;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherMcpFunction;

public static class AboutJsonResult
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IActionResult Create(AboutNode node)
        => new ContentResult
        {
            Content = JsonSerializer.Serialize(node, Options),
            ContentType = "application/json",
            StatusCode = 200,
        };
}
