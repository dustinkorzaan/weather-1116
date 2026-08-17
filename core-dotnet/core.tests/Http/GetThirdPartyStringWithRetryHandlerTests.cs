using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Http.Events;
using Core.Http.Handlers;

namespace Core.Tests.Http;

public class GetThirdPartyStringWithRetryHandlerTests
{
    private const string RequestUriBase = "https://api.open-meteo.com/v1/forecast";
    private const string SslFailure = "The SSL connection could not be established, see inner exception.";

    // Successful responses are cached in-process by request URI, so each test
    // that expects a real network hit needs its own unique URI.
    private static string UniqueRequestUri([System.Runtime.CompilerServices.CallerMemberName] string? testName = null) =>
        $"{RequestUriBase}?test={testName}&nonce={Guid.NewGuid():N}";

    [Fact]
    public async Task Handle_SucceedsOnFirstAttempt()
    {
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        var body = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = UniqueRequestUri() },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(1, handler.Attempts);
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
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        var body = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = UniqueRequestUri() },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(6, handler.Attempts);
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
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.Handle(
                new GetThirdPartyStringWithRetryEvent { RequestUri = UniqueRequestUri() },
                CancellationToken.None));

        Assert.Equal(SslFailure, thrown.Message);
        Assert.Equal(6, handler.Attempts);
    }

    [Fact]
    public async Task Handle_DoesNotRetryWhenCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new SequenceHandler([new HttpRequestException(SslFailure)]);
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.Handle(
                new GetThirdPartyStringWithRetryEvent { RequestUri = UniqueRequestUri() },
                cts.Token));

        Assert.InRange(handler.Attempts, 0, 1);
    }

    [Fact]
    public async Task Handle_SendsCustomHeaders()
    {
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        await sut.Handle(
            new GetThirdPartyStringWithRetryEvent
            {
                RequestUri = UniqueRequestUri(),
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
    public async Task Handle_CancelingTheCallerAbortsTheFetch()
    {
        var release = new TaskCompletionSource();
        var handler = new BlockingHandler(release.Task, "{\"ok\":true}");
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);
        var request = new GetThirdPartyStringWithRetryEvent { RequestUri = UniqueRequestUri() };
        using var cts = new CancellationTokenSource();

        var call = sut.Handle(request, cts.Token);
        await handler.RequestStarted;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
        Assert.Equal(1, handler.Attempts);
        Assert.False(release.Task.IsCompleted);
    }

    [Fact]
    public async Task Handle_ReturnsCachedResponseWithoutHittingNetworkAgain()
    {
        var requestUri = UniqueRequestUri();
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        using var client = new HttpClient(handler);
        var sut = new GetThirdPartyStringWithRetryHandler(client);

        var first = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);
        var second = await sut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", first);
        Assert.Equal("{\"ok\":true}", second);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task Handle_CachesAcrossDifferentHandlerInstancesForSameUri()
    {
        var requestUri = UniqueRequestUri();
        var firstHandler = new SequenceHandler(["{\"ok\":true}"]);
        using var firstClient = new HttpClient(firstHandler);
        var firstSut = new GetThirdPartyStringWithRetryHandler(firstClient);

        var firstResult = await firstSut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);

        var secondHandler = new SequenceHandler(["{\"ok\":false}"]);
        using var secondClient = new HttpClient(secondHandler);
        var secondSut = new GetThirdPartyStringWithRetryHandler(secondClient);

        var secondResult = await secondSut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", firstResult);
        Assert.Equal("{\"ok\":true}", secondResult);
        Assert.Equal(0, secondHandler.Attempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_DoesNotCacheNullOrWhitespaceResponses(string emptyBody)
    {
        var requestUri = UniqueRequestUri();
        var firstHandler = new SequenceHandler([emptyBody]);
        using var firstClient = new HttpClient(firstHandler);
        var firstSut = new GetThirdPartyStringWithRetryHandler(firstClient);

        await firstSut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);

        var secondHandler = new SequenceHandler(["{\"ok\":true}"]);
        using var secondClient = new HttpClient(secondHandler);
        var secondSut = new GetThirdPartyStringWithRetryHandler(secondClient);

        var secondResult = await secondSut.Handle(
            new GetThirdPartyStringWithRetryEvent { RequestUri = requestUri },
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", secondResult);
        Assert.Equal(1, secondHandler.Attempts);
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
