using System.Net;
using System.Text;
using Core.Caching;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Geo;

public class GeoHandlerRetryTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetLatLong_PermanentHttpStatus_IsNotRetried(HttpStatusCode statusCode)
    {
        var http = new ScriptedHandler(statusCode);
        var handler = new GetLatLongHandler(Cache(), Retry(), new FakeHttpClientFactory(http), NullLogger<GetLatLongHandler>.Instance);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.Handle(new GetLatLongEvent { Location = "Nashville" }, CancellationToken.None));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(1, http.Requests);
    }

    [Fact]
    public async Task GetLatLong_ServerError_IsRetried()
    {
        var http = new ScriptedHandler(
            HttpStatusCode.ServiceUnavailable,
            """{"results":[{"name":"Nashville","admin1":"Tennessee","country":"United States","latitude":36.16,"longitude":-86.78}]}""");
        var handler = new GetLatLongHandler(Cache(), Retry(), new FakeHttpClientFactory(http), NullLogger<GetLatLongHandler>.Instance);

        var response = await handler.Handle(new GetLatLongEvent { Location = "Nashville" }, CancellationToken.None);

        Assert.Equal("Nashville", response.Results[0].Name);
        Assert.Equal(2, http.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetLocation_PermanentHttpStatus_IsNotRetried(HttpStatusCode statusCode)
    {
        var http = new ScriptedHandler(statusCode);
        var handler = new GetLocationHandler(Cache(), Retry(), new FakeHttpClientFactory(http), NullLogger<GetLocationHandler>.Instance);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.Handle(new GetLocationEvent { Latitude = 36.16, Longitude = -86.78 }, CancellationToken.None));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(1, http.Requests);
    }

    [Fact]
    public async Task GetLocation_Throttled_IsRetried()
    {
        var http = new ScriptedHandler(
            HttpStatusCode.TooManyRequests,
            """{"name":"Nashville","address":{"city":"Nashville","state":"Tennessee","country_code":"us"}}""");
        var handler = new GetLocationHandler(Cache(), Retry(), new FakeHttpClientFactory(http), NullLogger<GetLocationHandler>.Instance);

        var response = await handler.Handle(new GetLocationEvent { Latitude = 36.16, Longitude = -86.78 }, CancellationToken.None);

        Assert.Equal("Nashville, Tennessee", response.Location);
        Assert.Equal(2, http.Requests);
    }

    private static CacheHelper Cache() => new(new MemoryCache(new MemoryCacheOptions()));

    private static TransientRetryHelper Retry() => new(NullLogger<TransientRetryHelper>.Instance);

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Answers the first request with <paramref name="firstStatus"/>, then every later one with
    /// 200 and <paramref name="okBody"/> (or <paramref name="firstStatus"/> again when no body is given).</summary>
    private sealed class ScriptedHandler(HttpStatusCode firstStatus, string? okBody = null) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (Requests == 1 || okBody is null)
            {
                return Task.FromResult(new HttpResponseMessage(firstStatus));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(okBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
