using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.Data;
using Core.Data.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherMcpSrvAppService.Tests;

public class UserToolsTests : IDisposable
{
    private const string McpKey = "integration-test-mcp-key";

    private readonly WeatherMcpSrvAppServiceWebApplicationFactory _factory;

    public UserToolsTests()
    {
        _factory = new WeatherMcpSrvAppServiceWebApplicationFactory()
            .WithSetting("MCP_SRV_APP_SERVICE_KEY", McpKey)
            .WithInMemoryDatabase();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WX1116DbContext>();
        dbContext.Users.Add(new User
        {
            Id = UserConstants.AnonymousUserId,
            FirstName = "Anonymous",
            UserCities =
            {
                new UserCity
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    UserId = UserConstants.AnonymousUserId,
                    Latitude = 36.1627,
                    Longitude = -86.7816,
                    LocationName = "Nashville, Tennessee",
                },
            },
        });
        dbContext.SaveChanges();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ToolsList_ServesOnlyTheUserTools()
    {
        using var result = await SendMcpAsync("tools/list", new { });

        var names = result.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()!)
            .Order()
            .ToArray();
        Assert.Equal(["AddUserCity", "DeleteUserCity", "GetUser"], names);
    }

    [Fact]
    public async Task GetUser_ReturnsSavedPins()
    {
        var cities = await GetCitiesAsync();

        var city = Assert.Single(cities);
        Assert.Equal("Nashville, Tennessee", city.GetProperty("locationName").GetString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", city.GetProperty("id").GetString());
    }

    [Fact]
    public async Task AddUserCity_ThenDeleteUserCity_RoundTrips()
    {
        using (var added = await CallToolAsync("AddUserCity", new { latitude = 30.2672, longitude = -97.7431, locationName = "Austin, Texas" }))
        {
            Assert.False(IsError(added));
        }

        var austin = (await GetCitiesAsync()).Single(city => city.GetProperty("locationName").GetString() == "Austin, Texas");

        using (var deleted = await CallToolAsync("DeleteUserCity", new { userCityId = austin.GetProperty("id").GetString() }))
        {
            Assert.False(IsError(deleted));
        }

        Assert.DoesNotContain(await GetCitiesAsync(), city => city.GetProperty("locationName").GetString() == "Austin, Texas");
    }

    [Fact]
    public async Task DeleteUserCity_UnknownId_ReturnsToolError()
    {
        using var result = await CallToolAsync("DeleteUserCity", new { userCityId = Guid.NewGuid() });

        Assert.True(IsError(result));
    }

    private async Task<List<JsonElement>> GetCitiesAsync()
    {
        using var result = await CallToolAsync("GetUser", new { });
        var text = result.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        using var user = JsonDocument.Parse(text);
        return user.RootElement.GetProperty("userCities").EnumerateArray().Select(city => city.Clone()).ToList();
    }

    private Task<JsonDocument> CallToolAsync(string name, object arguments) =>
        SendMcpAsync("tools/call", new { name, arguments });

    private static bool IsError(JsonDocument result) =>
        result.RootElement.GetProperty("result").TryGetProperty("isError", out var isError) && isError.GetBoolean();

    private async Task<JsonDocument> SendMcpAsync(string method, object parameters)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = parameters }),
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", McpKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var dataLine = body.Split('\n').FirstOrDefault(line => line.StartsWith("data: ", StringComparison.Ordinal));
        return JsonDocument.Parse(dataLine is null ? body : dataLine["data: ".Length..]);
    }
}
