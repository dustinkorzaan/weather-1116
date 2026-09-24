using System.Text.Json;
using Core.Geo.Events;
using Core.Json;
using Core.Users.Events;
using Core.Weather.Events;
using CQMediator;
using OpenAI.Responses;

namespace Core.Tools;

public sealed class WeatherToolExecutor
{
    private readonly IMediator _mediator;

    public WeatherToolExecutor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<string> ExecuteAsync(FunctionCallResponseItem functionCall, CancellationToken cancellationToken)
    {
        return functionCall.FunctionName switch
        {
            "GetLatLong" => await ExecuteGetLatLong(functionCall.FunctionArguments, cancellationToken),
            "GetLocation" => await ExecuteGetLocation(functionCall.FunctionArguments, cancellationToken),
            "GetCities" => await ExecuteGetCities(functionCall.FunctionArguments, cancellationToken),
            "GetPublicWeatherCurrent" => await ExecuteGetPublicWeatherCurrent(functionCall.FunctionArguments, cancellationToken),
            "GetPublicWeatherForecast" => await ExecuteGetPublicWeatherForecast(functionCall.FunctionArguments, cancellationToken),
            "GetPublicWeatherHistory" => await ExecuteGetPublicWeatherHistory(functionCall.FunctionArguments, cancellationToken),
            "GetUser" => await ExecuteGetUser(cancellationToken),
            "AddUserPin" => await ExecuteAddUserPin(functionCall.FunctionArguments, cancellationToken),
            "DeleteUserPin" => await ExecuteDeleteUserPin(functionCall.FunctionArguments, cancellationToken),
            _ => throw new NotImplementedException($"Unexpected tool call: {functionCall.FunctionName}"),
        };
    }

    private async Task<string> ExecuteGetLatLong(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        string location = argumentsJson.RootElement.GetProperty("location").GetString()
            ?? throw new InvalidOperationException("GetLatLong requires a location argument.");

        var latLongMatches = await _mediator.Send(new GetLatLongEvent { Location = location }, cancellationToken);
        return JsonSerializer.Serialize(latLongMatches, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteGetLocation(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

        var locationData = await _mediator.Send(new GetLocationEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);
        return JsonSerializer.Serialize(locationData, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteGetCities(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        var root = argumentsJson.RootElement;
        var citiesEvent = new GetCitiesEvent
        {
            Latitude = root.GetProperty("latitude").GetDouble(),
            Longitude = root.GetProperty("longitude").GetDouble(),
        };
        if (TryGetNumber(root, "radiusKm") is double radiusKm)
        {
            citiesEvent.RadiusKm = radiusKm;
        }
        if (TryGetNumber(root, "minPopulation") is double minPopulation)
        {
            citiesEvent.MinPopulation = (long)Math.Clamp(minPopulation, 0, long.MaxValue);
        }
        if (TryGetNumber(root, "maxCities") is double maxCities)
        {
            citiesEvent.MaxCities = (int)Math.Clamp(maxCities, int.MinValue, int.MaxValue);
        }

        // The city database can be unreachable or not yet imported; report that to the model instead of
        // failing the whole chat turn or AI weather request that happened to call GetCities.
        try
        {
            var cities = await _mediator.Send(citiesEvent, cancellationToken);
            return JsonSerializer.Serialize(cities, JsonDefaults.Pretty);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return JsonSerializer.Serialize(new { error = $"GetCities is unavailable right now: {ex.Message}" }, JsonDefaults.Pretty);
        }
    }

    private static double? TryGetNumber(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number ? element.GetDouble() : null;

    private async Task<string> ExecuteGetPublicWeatherCurrent(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

        var weatherData = await _mediator.Send(new GetPublicWeatherCurrentEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteGetPublicWeatherForecast(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
        var resolution = PublicWeatherForecastResolution.Daily;
        if (argumentsJson.RootElement.TryGetProperty("resolution", out var resolutionElement)
            && resolutionElement.GetString() is string resolutionText
            && Enum.TryParse(resolutionText, ignoreCase: true, out PublicWeatherForecastResolution parsedResolution))
        {
            resolution = parsedResolution;
        }

        var weatherData = await _mediator.Send(new GetPublicWeatherForecastEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            Resolution = resolution,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteGetPublicWeatherHistory(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
        var resolution = PublicWeatherHistoryResolution.Daily;
        if (argumentsJson.RootElement.TryGetProperty("resolution", out var resolutionElement)
            && resolutionElement.GetString() is string resolutionText
            && Enum.TryParse(resolutionText, ignoreCase: true, out PublicWeatherHistoryResolution parsedResolution))
        {
            resolution = parsedResolution;
        }

        var weatherData = await _mediator.Send(new GetPublicWeatherHistoryEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            Resolution = resolution,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteGetUser(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserEvent(), cancellationToken);
        return JsonSerializer.Serialize(user, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteAddUserPin(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();
        string locationName = argumentsJson.RootElement.GetProperty("locationName").GetString()
            ?? throw new InvalidOperationException("AddUserPin requires a locationName argument.");

        await _mediator.Send(new AddUserPinEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            LocationName = locationName,
        }, cancellationToken);

        return JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
    }

    private async Task<string> ExecuteDeleteUserPin(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        string userPinId = argumentsJson.RootElement.GetProperty("userPinId").GetString()
            ?? throw new InvalidOperationException("DeleteUserPin requires a userPinId argument.");

        if (!Guid.TryParse(userPinId, out var pinId))
        {
            throw new InvalidOperationException("userPinId must be a valid GUID.");
        }

        await _mediator.Send(new DeleteUserPinEvent
        {
            UserPinId = pinId,
        }, cancellationToken);

        return JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
    }
}
