using Core.Weather.Events;
using Core.Weather.Handlers;
using Core.Weather.Models;
using MediatR;

namespace Core.Tests.Weather;

public class GetUIWeatherHistoryHandlerTests
{
    [Fact]
    public async Task Handle_MapsMetricResponseToUSCustomaryUnits()
    {
        var metric = new NonAIHistoryWeatherResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Hourly = new NonAIHistoryWeatherHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2mC = [24],
                PrecipitationMm = [25.4],
                WindSpeed10mKmh = [10],
                WindDirectionSource10m = [180],
                WeatherCode = [1],
            },
        };
        var mediator = new FakeMediator(metric);
        var handler = new GetUIWeatherHistoryHandler(mediator);

        var response = await handler.Handle(
            new GetUIWeatherHistoryEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherHistoryResolution.Hourly },
            CancellationToken.None);

        Assert.Equal(36.16, mediator.LastLatitude);
        Assert.Equal(-86.78, mediator.LastLongitude);
        Assert.Equal(PublicWeatherHistoryResolution.Hourly, mediator.LastResolution);
        Assert.NotNull(response.Hourly);
        Assert.Equal([75.2], response.Hourly!.TemperatureF);
        Assert.Equal([1], response.Hourly.PrecipitationInch);
        Assert.Equal([180], response.Hourly.WindDirectionSourceDegrees);
    }

    private sealed class FakeMediator(NonAIHistoryWeatherResponse response) : IMediator
    {
        public double? LastLatitude { get; private set; }

        public double? LastLongitude { get; private set; }

        public PublicWeatherHistoryResolution? LastResolution { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetPublicWeatherHistoryEvent historyEvent)
            {
                LastLatitude = historyEvent.Latitude;
                LastLongitude = historyEvent.Longitude;
                LastResolution = historyEvent.Resolution;
            }

            return Task.FromResult((TResponse)(object)response);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
