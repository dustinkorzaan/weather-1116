using Core.AIWeather.Events;
using Core.AIWeather.Handlers;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.AIWeather.Handlers;

public class ConfirmNashvilleAIWeatherHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsConfirmedValidResponse()
    {
        var mediator = new FakeMediator(new AIWeatherResponse
        {
            FullSummary = "It is 72F in Nashville with light winds from the south.",
            TemperatureF = 72,
            WindSpeedMPH = 5,
            WindDirectionSource = "S",
            Conditions = "Sunny",
        });
        var handler = new ConfirmNashvilleAIWeatherHandler(
            mediator,
            NullLogger<ConfirmNashvilleAIWeatherHandler>.Instance);

        var response = await handler.Handle(new ConfirmNashvilleAIWeatherEvent(), CancellationToken.None);

        Assert.Equal("It is 72F in Nashville with light winds from the south.", response.FullSummary);
        Assert.Equal("Nashville, TN", mediator.LastLocation);
    }

    [Fact]
    public async Task Handle_ThrowsWhenResponseIsMissingSummary()
    {
        var mediator = new FakeMediator(new AIWeatherResponse
        {
            Conditions = "Sunny",
        });
        var handler = new ConfirmNashvilleAIWeatherHandler(
            mediator,
            NullLogger<ConfirmNashvilleAIWeatherHandler>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new ConfirmNashvilleAIWeatherEvent(), CancellationToken.None));

        Assert.Contains("fullSummary", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeMediator(AIWeatherResponse response) : IMediator
    {
        public string? LastLocation { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request is GetCurrentAIWeatherEvent aiWeatherEvent)
            {
                LastLocation = aiWeatherEvent.Location;
            }

            return Task.FromResult((TResponse)(object)response);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
