using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Http;
using Core.Http.Events;
using Core.Http.Handlers;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Tests.Http;

public class GetThirdPartyStringWithRetryHandlerTests
{
    private const string RequestUri = "https://api.open-meteo.com/v1/forecast";
    private const string SslFailure = "The SSL connection could not be established, see inner exception.";

    private static readonly TimeSpan[] ExpectedRetryDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(400),
        TimeSpan.FromMilliseconds(800),
        TimeSpan.FromMilliseconds(1600),
        TimeSpan.FromMilliseconds(3200),
    ];

    [Fact]
    public void DelayBeforeRetry_StartsAt200MsAndDoublesFiveTimes()
    {
        Assert.Equal(5, GetThirdPartyStringWithRetryHandler.RetryCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), GetThirdPartyStringWithRetryHandler.InitialRetryDelay);
        Assert.Equal(
            ExpectedRetryDelays,
            Enumerable.Range(0, GetThirdPartyStringWithRetryHandler.RetryCount)
                .Select(GetThirdPartyStringWithRetryHandler.DelayBeforeRetry));
    }

    [Fact]
    public async Task Handle_SucceedsOnFirstAttempt_WithoutDelay()
    {
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays);

        var body = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(1, handler.Attempts);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task Handle_RetriesSslFailureFiveTimesThenSucceeds()
    {
        var handler = new SequenceHandler(
            [
                new HttpRequestException(SslFailure),
                new HttpRequestException(SslFailure),
                new HttpRequestException(SslFailure),
                new HttpRequestException(SslFailure),
                new HttpRequestException(SslFailure),
                "{\"ok\":true}",
            ]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays);

        var body = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(6, handler.Attempts);
        Assert.Equal(ExpectedRetryDelays, delays);
    }

    [Fact]
    public async Task Handle_ThrowsAfterFiveRetries()
    {
        var handler = new SequenceHandler(
            [
                new HttpRequestException("first"),
                new IOException("second"),
                new SocketException(),
                new HttpRequestException(SslFailure),
                new HttpRequestException("fourth"),
                new HttpRequestException(SslFailure),
            ]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays);

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.Handle(
                new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri },
                CancellationToken.None));

        Assert.Equal(SslFailure, thrown.Message);
        Assert.Equal(6, handler.Attempts);
        Assert.Equal(ExpectedRetryDelays, delays);
    }

    [Fact]
    public async Task Handle_DoesNotRetryWhenCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new SequenceHandler([new HttpRequestException(SslFailure)]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.Handle(
                new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri },
                cts.Token));

        Assert.InRange(handler.Attempts, 0, 1);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task Handle_SendsCustomHeaders()
    {
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays);

        await sut.Handle(
            new GetThirdPartyStringWithRetryEvent
            {
                RequestUri = RequestUri,
                Headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = "Weather-1116/1.0",
                    ["Accept"] = "application/json",
                },
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Weather-1116/1.0", handler.LastRequest!.Headers.UserAgent.ToString());
        Assert.Equal("application/json", handler.LastRequest.Headers.Accept.ToString());
    }

    [Fact]
    public async Task Handle_CachesSuccessfulResponseByRequestUri()
    {
        var handler = new SequenceHandler(["{\"ok\":true}", "{\"ok\":false}"]);
        var delays = new List<TimeSpan>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays, cache);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri };

        var first = await sut.Handle(request, CancellationToken.None);
        var second = await sut.Handle(request, CancellationToken.None);

        Assert.Equal("{\"ok\":true}", first);
        Assert.Equal("{\"ok\":true}", second);
        Assert.Equal(1, handler.Attempts);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task Handle_DoesNotShareCacheAcrossDifferentRequestUris()
    {
        var handler = new SequenceHandler(["{\"first\":true}", "{\"second\":true}"]);
        var delays = new List<TimeSpan>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays, cache);

        var first = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri + "?one=1" },
            CancellationToken.None);
        var second = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri + "?two=2" },
            CancellationToken.None);

        Assert.Equal("{\"first\":true}", first);
        Assert.Equal("{\"second\":true}", second);
        Assert.Equal(2, handler.Attempts);
    }

    [Fact]
    public async Task Handle_DoesNotCacheFailures()
    {
        var handler = new SequenceHandler(
            [
                new HttpRequestException("first"),
                new HttpRequestException("second"),
                new HttpRequestException("third"),
                new HttpRequestException("fourth"),
                new HttpRequestException("fifth"),
                new HttpRequestException("final"),
                "{\"ok\":true}",
            ]);
        var delays = new List<TimeSpan>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, delays, cache);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.Handle(request, CancellationToken.None));

        var body = await sut.Handle(request, CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(7, handler.Attempts);
    }

    [Fact]
    public async Task Handle_CoalescesConcurrentRequestsForSameUri()
    {
        var release = new TaskCompletionSource();
        var handler = new BlockingHandler(release.Task, "{\"ok\":true}");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var coalescer = new ThirdPartyRequestCoalescer();
        using var client = new HttpClient(handler);
        var sut1 = CreateSut(client, [], cache, coalescer);
        var sut2 = CreateSut(client, [], cache, coalescer);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri };

        var firstCall = sut1.Handle(request, CancellationToken.None);
        await handler.RequestStarted;
        var secondCall = sut2.Handle(request, CancellationToken.None);
        release.SetResult();

        var results = await Task.WhenAll(firstCall, secondCall);

        Assert.Equal(["{\"ok\":true}", "{\"ok\":true}"], results);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task Handle_CancelingOneJoinedCallerDoesNotAbortSharedFetchForOthers()
    {
        var release = new TaskCompletionSource();
        var handler = new BlockingHandler(release.Task, "{\"ok\":true}");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var coalescer = new ThirdPartyRequestCoalescer();
        using var client = new HttpClient(handler);
        var sut1 = CreateSut(client, [], cache, coalescer);
        var sut2 = CreateSut(client, [], cache, coalescer);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri };
        using var cts = new CancellationTokenSource();

        var firstCall = sut1.Handle(request, cts.Token);
        await handler.RequestStarted;
        var secondCall = sut2.Handle(request, CancellationToken.None);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstCall);

        release.SetResult();
        var secondResult = await secondCall;

        Assert.Equal("{\"ok\":true}", secondResult);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task Handle_CancelingTheOnlyCallerAbortsTheFetch()
    {
        var release = new TaskCompletionSource();
        var handler = new BlockingHandler(release.Task, "{\"ok\":true}");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler);
        var sut = CreateSut(client, [], cache);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = RequestUri };
        using var cts = new CancellationTokenSource();

        var call = sut.Handle(request, cts.Token);
        await handler.RequestStarted;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
        Assert.Equal(1, handler.Attempts);
        Assert.False(release.Task.IsCompleted);
    }

    [Theory]
    [InlineData("core-dotnet/core/Geo/Handlers/GetLatLongHandler.cs")]
    [InlineData("core-dotnet/core/Geo/Handlers/GetLocationHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherCurrentHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherForecastHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherHistoryHandler.cs")]
    public void ThirdPartyHttpsHandlers_UseSharedRetryEvent(string relativePath)
    {
        var source = File.ReadAllText(FindRepoFile(relativePath));
        Assert.Contains("GetThirdPartyStringWithRetryEvent", source);
        Assert.Contains("_mediator.Send", source);
        Assert.DoesNotContain("new HttpClient", source);
        Assert.DoesNotContain("GetStringAsync", source);
    }

    private static GetThirdPartyStringWithRetryHandler CreateSut(
        HttpClient client,
        List<TimeSpan> delays,
        IMemoryCache? cache = null,
        ThirdPartyRequestCoalescer? coalescer = null) =>
        new(
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            coalescer ?? new ThirdPartyRequestCoalescer(),
            client,
            RecordDelay(delays));

    private static Func<TimeSpan, CancellationToken, Task> RecordDelay(List<TimeSpan> delays) =>
        (delay, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            delays.Add(delay);
            return Task.CompletedTask;
        };

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}");
    }

    private sealed class SequenceHandler(IEnumerable<object> outcomes) : HttpMessageHandler
    {
        private readonly Queue<object> _outcomes = new(outcomes);

        public int Attempts { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            LastRequest = request;
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = _outcomes.Dequeue();
            if (outcome is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent((string)outcome, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class BlockingHandler(Task release, string body) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _started = new();

        public int Attempts { get; private set; }

        public Task RequestStarted => _started.Task;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            _started.TrySetResult();
            await release.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
