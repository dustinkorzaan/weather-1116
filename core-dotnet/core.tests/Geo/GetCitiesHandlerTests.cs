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
    [InlineData(0, GetCitiesEvent.MinDistanceKm)]
    [InlineData(-50, GetCitiesEvent.MinDistanceKm)]
    [InlineData(5000, GetCitiesEvent.MaxDistanceKm)]
    [InlineData(double.PositiveInfinity, GetCitiesEvent.MaxDistanceKm)]
    [InlineData(double.NaN, GetCitiesEvent.DefaultDistanceKm)]
    [InlineData(250.5, 250.5)]
    public void NormalizeDistanceKm_ResetsIntoRange(double input, double expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeDistanceKm(input));

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(500, GetCitiesEvent.MaxSize)]
    [InlineData(40, 40)]
    public void NormalizeSize_ResetsIntoRange(int input, int expected) =>
        Assert.Equal(expected, GetCitiesEvent.NormalizeSize(input));

    [Fact]
    public void Event_Defaults_Are161KmAnd25Cities()
    {
        var request = new GetCitiesEvent { Latitude = 36.1627, Longitude = -86.7816 };

        Assert.Equal(161, request.DistanceKm);
        Assert.Equal(25, request.Size);
    }

    [Fact]
    public void BuildNearbyCitiesUrl_UsesEscapedIso6709LocationAndPopulationSort()
    {
        var url = GetCitiesHandler.BuildNearbyCitiesUrl(36.1627, -86.7816, 161, 10, 20);

        Assert.StartsWith("https://geodb-free-service.wirefreethought.com/v1/geo/locations/%2B36.1627-086.7816/nearbyCities?", url);
        Assert.Contains("radius=161", url);
        Assert.Contains("distanceUnit=KM", url);
        Assert.Contains("types=CITY", url);
        Assert.Contains("sort=-population", url);
        Assert.Contains("limit=10", url);
        Assert.Contains("offset=20", url);
    }

    [Fact]
    public async Task Handle_SizeZero_ReturnsEmptyWithoutCallingGeoDb()
    {
        var http = new PagingHandler(totalCount: 50);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, Size = -3 },
            CancellationToken.None);

        Assert.Empty(response.Cities);
        Assert.Equal(0, response.Size);
        Assert.Empty(http.RequestedUrls);
    }

    [Fact]
    public async Task Handle_OutOfRangeInputs_ResetsInsteadOfFailing()
    {
        var http = new PagingHandler(totalCount: 500);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, DistanceKm = 20000, Size = 1000 },
            CancellationToken.None);

        Assert.Equal(GetCitiesHandler.GeoDbMaxRadiusKm, response.DistanceKm);
        Assert.Equal(GetCitiesEvent.MaxSize, response.Size);
        Assert.Equal(GetCitiesEvent.MaxSize, response.Returned);
        Assert.All(http.RequestedUrls, url => Assert.Contains("radius=100&", url));
    }

    [Theory]
    [InlineData(GetCitiesEvent.DefaultDistanceKm, 100)]
    [InlineData(GetCitiesEvent.MaxDistanceKm, 100)]
    [InlineData(50, 50)]
    public async Task Handle_CapsRadiusAtGeoDbFreeTierMaximum(double requested, double sent)
    {
        var http = new PagingHandler(totalCount: 5);
        var handler = CreateHandler(http);

        var response = await handler.Handle(
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, DistanceKm = requested },
            CancellationToken.None);

        Assert.Equal(sent, response.DistanceKm);
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
    public async Task Handle_PagesTenAtATimeUntilSizeIsReached()
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
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, Size = 100 },
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
            new GetCitiesEvent { Latitude = 36.16, Longitude = -86.78, Size = 1 },
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
            ResponseItem.CreateFunctionCallItem("call-1", "GetCities", BinaryData.FromString("""{"latitude":36.16,"longitude":-86.78,"distanceKM":300,"size":null}""")),
            CancellationToken.None);

        var request = Assert.IsType<GetCitiesEvent>(mediator.LastRequest);
        Assert.Equal(36.16, request.Latitude);
        Assert.Equal(-86.78, request.Longitude);
        Assert.Equal(300, request.DistanceKm);
        Assert.Equal(GetCitiesEvent.DefaultSize, request.Size);
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
