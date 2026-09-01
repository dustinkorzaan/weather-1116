using Core.Weather.Events;
using Core.Weather.Models;
using CQMediator;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches the metric public history and maps it to the UI-facing response in US customary units.
/// </summary>
public class GetUIWeatherHistoryHandler : IRequestHandler<GetUIWeatherHistoryEvent, UIWeatherHistoryResponse>
{
    private readonly IMediator _mediator;

    public GetUIWeatherHistoryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<UIWeatherHistoryResponse> Handle(GetUIWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        var metric = await _mediator.Send(
            new GetPublicWeatherHistoryEvent
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Resolution = request.Resolution,
            },
            cancellationToken);

        return WeatherResponseMapper.ToUIHistoryResponse(metric);
    }
}
