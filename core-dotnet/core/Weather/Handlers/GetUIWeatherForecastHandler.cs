using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches the metric public forecast and maps it to the UI-facing response in US customary units.
/// </summary>
public class GetUIWeatherForecastHandler : IRequestHandler<GetUIWeatherForecastEvent, UIWeatherForecastResponse>
{
    private readonly IMediator _mediator;

    public GetUIWeatherForecastHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<UIWeatherForecastResponse> Handle(GetUIWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        var metric = await _mediator.Send(
            new GetPublicWeatherForecastEvent
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Resolution = request.Resolution,
            },
            cancellationToken);

        return WeatherResponseMapper.ToUIForecastResponse(metric);
    }
}
