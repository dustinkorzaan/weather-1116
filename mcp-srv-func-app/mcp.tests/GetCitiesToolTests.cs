using Core.Geo.Events;
using Core.Geo.Models;
using CQMediator;

namespace WeatherMcpSrvFuncApp.Tests;

public class GetCitiesToolTests
{
    [Fact]
    public async Task GetCities_UsesEventDefaults_WhenOptionalArgumentsOmitted()
    {
        var mediator = new CapturingMediator();
        var tool = new GetCitiesTool(mediator);

        await tool.GetCities(null!, 36.16, -86.78, null, null, null);

        var sent = Assert.IsType<GetCitiesEvent>(mediator.Request);
        Assert.Equal(36.16, sent.Latitude);
        Assert.Equal(-86.78, sent.Longitude);
        Assert.Equal(GetCitiesEvent.DefaultRadiusKm, sent.RadiusKm);
        Assert.Equal(GetCitiesEvent.DefaultMinPopulation, sent.MinPopulation);
        Assert.Equal(GetCitiesEvent.DefaultMaxCities, sent.MaxCities);
    }

    [Fact]
    public async Task GetCities_PassesProvidedArgumentsThrough()
    {
        var mediator = new CapturingMediator();
        var tool = new GetCitiesTool(mediator);

        await tool.GetCities(null!, 36.16, -86.78, 50, 50000, 12);

        var sent = Assert.IsType<GetCitiesEvent>(mediator.Request);
        Assert.Equal(50, sent.RadiusKm);
        Assert.Equal(50000, sent.MinPopulation);
        Assert.Equal(12, sent.MaxCities);
    }

    private sealed class CapturingMediator : IMediator
    {
        public object? Request { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult((TResponse)(object)new NonAICitiesResponse());
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
