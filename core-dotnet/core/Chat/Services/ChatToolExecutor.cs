using System.Text.Json;
using Core.Geo.Events;
using Core.Weather.Events;
using MediatR;
using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatToolExecutor
{
    private readonly IMediator _mediator;

    public ChatToolExecutor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<string> ExecuteAsync(FunctionCallResponseItem functionCall, CancellationToken cancellationToken)
    {
        return functionCall.FunctionName switch
        {
            "GetLatLongData" => await ExecuteGetLatLong(functionCall.FunctionArguments, cancellationToken),
            "GetLocationData" => await ExecuteGetLocation(functionCall.FunctionArguments, cancellationToken),
            "GetPublicWeatherData" => await ExecuteGetPublicWeather(functionCall.FunctionArguments, cancellationToken),
            _ => throw new NotImplementedException($"Unexpected tool call: {functionCall.FunctionName}"),
        };
    }

    private async Task<string> ExecuteGetLatLong(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        string location = argumentsJson.RootElement.GetProperty("location").GetString()
            ?? throw new InvalidOperationException("GetLatLongData requires a location argument.");

        var latLongMatches = await _mediator.Send(new GetLatLongDataEvent { Location = location }, cancellationToken);
        return JsonSerializer.Serialize(latLongMatches, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> ExecuteGetLocation(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

        var locationData = await _mediator.Send(new GetLocationDataEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);
        return JsonSerializer.Serialize(locationData, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> ExecuteGetPublicWeather(BinaryData arguments, CancellationToken cancellationToken)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(arguments);
        double latitude = argumentsJson.RootElement.GetProperty("latitude").GetDouble();
        double longitude = argumentsJson.RootElement.GetProperty("longitude").GetDouble();

        var weatherData = await _mediator.Send(new GetPublicWeatherDataEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
    }
}
