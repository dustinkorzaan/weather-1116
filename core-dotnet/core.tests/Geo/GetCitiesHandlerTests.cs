using System.Net;
using System.Text;
using System.Text.Json;
using Core.Caching;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Http;
using Core.Tools;
using CQMediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
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
    [InlineData(-5, 0)]
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

    [Fact]
    public void BuildNearbyCitiesUrl_UsesEscapedIso6709LocationAndPopulationSort()
    {
        var url = GetCitiesHandler.BuildNearbyCitiesUrl(36.1627, -86.7816, 161, 0, 10, 20);

        Assert.StartsWith("https://geodb-free-service.wirefreethought.com/v1/geo/locations/%2B36.1627-086.7816/nearbyCities?", url);
        Assert.Contains("radius=161", url);
        Assert.Contains("distanceUnit=KM", url);
        Assert.Contains("types=CITY", url);
        Assert.Contains("sort=-population", url);
        Assert.Contains("limit=10", url);
        Assert.Contains("offset=20", url);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(50000, 50000)]
    public void NormalizeMinPopulation_ResetsNegativeToZero(long input, long expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeMinPopulation(input));

    [Fact]
    public void BuildNearbyCitiesUrl_AddsMinPopulationOnlyWhenPositive()
    {
        Assert.DoesNotContain("minPopulation", GetCitiesHandler.BuildNearbyCitiesUrl(36.1627, -86.7816, 100, 0, 10, 0));
        Assert.Contains("minPopulation=50000", GetCitiesHandler.BuildNearbyCitiesUrl(36.1627, -86.7816, 100, 50000, 10, 0));
    }

    [Fact]
    public async Task Handle_MaxCitiesZero_ReturnsEmptyWithoutCallingGeoDb()
    {
        var http = new PagingHandler(totalCount: 50);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, MaxCities = -3 },
            CancellationToken.None);

        Assert.Empty(response.Cities);
        Assert.Equal(0, response.MaxCities);
        Assert.Empty(http.RequestedUrls);
    }

    [Fact]
    public async Task Handle_OutOfRangeInputs_ResetsInsteadOfFailing()
    {
        var http = new PagingHandler(totalCount: 500);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, RadiusKm = 20000, MinPopulation = -10, MaxCities = 1000 },
            CancellationToken.None);

        Assert.Equal(GetCitiesHandler.GeoDbMaxRadiusKm, response.RadiusKm);
        Assert.Equal(0, response.MinPopulation);
        Assert.Equal(GetCitiesEvent.MaxMaxCities, response.MaxCities);
        Assert.Equal(GetCitiesEvent.MaxMaxCities, response.Returned);
        Assert.All(http.RequestedUrls, url => Assert.Contains("radius=100&", url));
    }

    [Theory]
    [InlineData(GetCitiesEvent.DefaultRadiusKm, 100)]
    [InlineData(GetCitiesEvent.MaxRadiusKm, 100)]
    [InlineData(50, 50)]
    public async Task Handle_CapsRadiusAtGeoDbFreeTierMaximum(double requested, double sent)
    {
        var http = new PagingHandler(totalCount: 5);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, RadiusKm = requested },
            CancellationToken.None);

        Assert.Equal(sent, response.RadiusKm);
        Assert.Contains(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"radius={sent}&"), http.RequestedUrls[0]);
    }

    [Fact]
    public async Task Handle_GeoDbAccessDenied_IsNotRetried()
    {
        var http = new PagingHandler(totalCount: 5, status: HttpStatusCode.Forbidden);
        var handler = CreateHandler(http);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78 },
            CancellationToken.None));

        Assert.Single(http.RequestedUrls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    public void IsPermanentFailure_ClassifiesStatusCodes(HttpStatusCode statusCode, bool expected) =>
        Assert.Equal(expected, GetCitiesHandler.IsPermanentFailure(statusCode));

    [Fact]
    public async Task Handle_PagesTenAtATimeUntilMaxCitiesIsReached()
    {
        var http = new PagingHandler(totalCount: 500);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78 },
            CancellationToken.None);

        Assert.Equal(25, response.Returned);
        Assert.Equal(500, response.TotalAvailable);
        Assert.Equal(3, http.RequestedUrls.Count);
        Assert.Contains("limit=10&offset=0", http.RequestedUrls[0]);
        Assert.Contains("limit=10&offset=10", http.RequestedUrls[1]);
        Assert.Contains("limit=5&offset=20", http.RequestedUrls[2]);
    }

    [Fact]
    public async Task Handle_StopsWhenGeoDbRunsOutOfCities()
    {
        var http = new PagingHandler(totalCount: 12);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, MaxCities = 100 },
            CancellationToken.None);

        Assert.Equal(12, response.Returned);
        Assert.Equal(12, response.TotalAvailable);
        Assert.Equal(2, http.RequestedUrls.Count);
    }

    [Fact]
    public async Task Handle_MapsGeoDbFields()
    {
        var http = new PagingHandler(totalCount: 1);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, MaxCities = 1 },
            CancellationToken.None);

        var city = Assert.Single(response.Cities);
        Assert.Equal("City 0", city.Name);
        Assert.Equal("Tennessee", city.Region);
        Assert.Equal("United States of America", city.Country);
        Assert.Equal(36.1, city.Latitude);
        Assert.Equal(-86.7, city.Longitude);
        Assert.Equal(12.5, city.DistanceKm);
        Assert.Equal(100000, city.Population);
    }

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
        Assert.Contains("GeoDB rejected", json.RootElement.GetProperty("error").GetString());
    }

    private static GetCitiesHandler CreateHandler(HttpMessageHandler http) =>
        new(
            new CacheHelper(new MemoryCache(new MemoryCacheOptions())),
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(http),
            NullLogger<GetCitiesHandler>.Instance)
        {
            PageDelay = TimeSpan.Zero,
        };

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Serves GeoDB-shaped pages from a fixed-size result set, honoring limit/offset.</summary>
    private sealed class PagingHandler(int totalCount, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            if (status != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(status));
            }

            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var limit = int.Parse(query["limit"]!);
            var offset = int.Parse(query["offset"]!);
            var data = Enumerable.Range(offset, Math.Max(0, Math.Min(limit, totalCount - offset)))
                .Select(i => new GeoDbCity
                {
                    Name = $"City {i}",
                    Region = "Tennessee",
                    Country = "United States of America",
                    Latitude = 36.1,
                    Longitude = -86.7,
                    Distance = 12.5,
                    Population = 100000 - i,
                })
                .ToList();
            var body = JsonSerializer.Serialize(new GeoDbNearbyCitiesResponse
            {
                Data = data,
                Metadata = new GeoDbMetadata { CurrentOffset = offset, TotalCount = totalCount },
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class ThrowingMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("GeoDB rejected the request with HTTP 403.");

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
