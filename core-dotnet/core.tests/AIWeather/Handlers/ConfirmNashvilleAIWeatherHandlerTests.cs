using Core.AIWeather.Events;
using Core.AIWeather.Handlers;
using Core.AIWeather.Models;
using CQMediator;
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

        var response = await handler.Handle(new ConfirmNashvilleAIWeatherEvent { Version = 3 }, CancellationToken.None);

        Assert.Equal("It is 72F in Nashville with light winds from the south.", response.FullSummary);
        Assert.Equal("Nashville, TN", mediator.LastLocation);
        Assert.Equal(typeof(GetCurrentAIWeatherV3Event), mediator.LastEventType);
    }

    [Fact]
    public async Task Handle_Version4_SendsGetCurrentAIWeatherV4Event()
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

        var response = await handler.Handle(new ConfirmNashvilleAIWeatherEvent { Version = 4 }, CancellationToken.None);

        Assert.Equal("It is 72F in Nashville with light winds from the south.", response.FullSummary);
        Assert.Equal("Nashville, TN", mediator.LastLocation);
        Assert.Equal(typeof(GetCurrentAIWeatherV4Event), mediator.LastEventType);
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
            () => handler.Handle(new ConfirmNashvilleAIWeatherEvent { Version = 3 }, CancellationToken.None));

        Assert.Contains("fullSummary", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeMediator(AIWeatherResponse response) : IMediator
    {
        public string? LastLocation { get; private set; }

        public Type? LastEventType { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastEventType = request.GetType();

            if (request is GetCurrentAIWeatherV3Event v3Event)
            {
                LastLocation = v3Event.Location;
            }
            else if (request is GetCurrentAIWeatherV4Event v4Event)
            {
                LastLocation = v4Event.Location;
            }

            return Task.FromResult((TResponse)(object)response);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
