using Core.Weather.Events;
using Core.Weather.Handlers;
using Core.Weather.Models;
using MediatR;

namespace Core.Tests.Weather;

public class GetUIWeatherForecastHandlerTests
{
    [Fact]
    public async Task Handle_MapsMetricResponseToUSCustomaryUnits()
    {
        var metric = new PublicWeatherForecastResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new PublicWeatherForecastDaily
            {
                Time = ["2026-08-16"],
                Temperature2mMax = [24],
                Temperature2mMin = [0],
                PrecipitationSum = [25.4],
                WindSpeed10mMax = [10],
                WindDirectionSource10mDominant = [224],
                WeatherCode = [2],
            },
        };
        var mediator = new FakeMediator(metric);
        var handler = new GetUIWeatherForecastHandler(mediator);

        var response = await handler.Handle(
            new GetUIWeatherForecastEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherForecastResolution.Daily },
            CancellationToken.None);

        Assert.Equal(36.16, mediator.LastLatitude);
        Assert.Equal(-86.78, mediator.LastLongitude);
        Assert.Equal(PublicWeatherForecastResolution.Daily, mediator.LastResolution);
        Assert.NotNull(response.Daily);
        Assert.Equal([75.2], response.Daily!.TemperatureHighF);
        Assert.Equal([32], response.Daily.TemperatureLowF);
    }

    private sealed class FakeMediator(PublicWeatherForecastResponse response) : IMediator
    {
        public double? LastLatitude { get; private set; }

        public double? LastLongitude { get; private set; }

        public PublicWeatherForecastResolution? LastResolution { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetPublicWeatherForecastEvent forecastEvent)
            {
                LastLatitude = forecastEvent.Latitude;
                LastLongitude = forecastEvent.Longitude;
                LastResolution = forecastEvent.Resolution;
            }

            return Task.FromResult((TResponse)(object)response);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
