using System.Text.Json;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Tools;
using CQMediator;
using OpenAI.Responses;

namespace Core.Tests.Geo;

public class GetCitiesHandlerTests
{
    [Theory]
    [InlineData(0, GetCitiesEvent.MinRadiusKm)]
    [InlineData(-50, GetCitiesEvent.MinRadiusKm)]
    [InlineData(5000, GetCitiesEvent.MaxRadiusKm)]
    [InlineData(double.PositiveInfinity, GetCitiesEvent.MaxRadiusKm)]
    [InlineData(double.NaN, GetCitiesEvent.DefaultRadiusKm)]
    [InlineData(250.5, 250.5)]
    public void NormalizeRadiusKm_ResetsIntoRange(double input, double expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeRadiusKm(input));

    [Theory]
    [InlineData(-5, GetCitiesEvent.MinMaxCities)]
    [InlineData(0, GetCitiesEvent.MinMaxCities)]
    [InlineData(500, GetCitiesEvent.MaxMaxCities)]
    [InlineData(40, 40)]
    public void NormalizeMaxCities_ResetsIntoRange(int input, int expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeMaxCities(input));

    [Fact]
    public void Event_Defaults_Are161KmNoPopulationFloorAnd25Cities()
    {
        var request = new GetCitiesEvent { Latitude = 36.1627, Longitude = -86.7816 };

        Assert.Equal(161, request.RadiusKm);
        Assert.Equal(0, request.MinPopulation);
        Assert.Equal(25, request.MaxCities);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(50000, 50000)]
    public void NormalizeMinPopulation_ResetsNegativeToZero(long input, long expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeMinPopulation(input));

    [Fact]
    public void ExcludedFeatureCodes_AreCitySectionsAndHistoricalPlaces() =>
        Assert.Equal(["PPLX", "PPLH", "PPLQ", "PPLW"], GetCitiesHandler.ExcludedFeatureCodes);

    [Fact]
    public async Task WeatherToolExecutor_GetCities_PassesArgumentsAndNullDefaults()
    {
        var mediator = new RecordingMediator();
        var executor = new WeatherToolExecutor(mediator);

        await executor.ExecuteAsync(
            ResponseItem.CreateFunctionCallItem("call-1", "GetCities", BinaryData.FromString("""{"latitude":36.16,"longitude":-86.78,"radiusKm":300,"minPopulation":50000,"maxCities":null}""")),
            CancellationToken.None);

        var request = Assert.IsType<GetCitiesEvent>(mediator.LastRequest);
        Assert.Equal(36.16, request.Latitude);
        Assert.Equal(-86.78, request.Longitude);
        Assert.Equal(300, request.RadiusKm);
        Assert.Equal(50000, request.MinPopulation);
        Assert.Equal(GetCitiesEvent.DefaultMaxCities, request.MaxCities);
    }

    [Fact]
    public async Task WeatherToolExecutor_GetCitiesFailure_ReturnsErrorJsonInsteadOfThrowing()
    {
        var executor = new WeatherToolExecutor(new ThrowingMediator());

        var output = await executor.ExecuteAsync(
            ResponseItem.CreateFunctionCallItem("call-1", "GetCities", BinaryData.FromString("""{"latitude":36.16,"longitude":-86.78,"radiusKm":null,"minPopulation":null,"maxCities":null}""")),
            CancellationToken.None);

        using var json = JsonDocument.Parse(output);
        Assert.Contains("City database is unavailable", json.RootElement.GetProperty("error").GetString());
    }

    private sealed class ThrowingMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("City database is unavailable.");

        public Task Send(IRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingMediator : IMediator
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)new NonAICitiesResponse());
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
