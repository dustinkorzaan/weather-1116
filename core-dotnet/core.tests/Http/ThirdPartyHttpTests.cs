using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Http;

namespace Core.Tests.Http;

public class ThirdPartyHttpTests
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
        Assert.Equal(5, ThirdPartyHttp.RetryCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), ThirdPartyHttp.InitialRetryDelay);
        Assert.Equal(
            ExpectedRetryDelays,
            Enumerable.Range(0, ThirdPartyHttp.RetryCount).Select(ThirdPartyHttp.DelayBeforeRetry));
    }

    [Fact]
    public async Task GetStringWithRetryAsync_SucceedsOnFirstAttempt_WithoutDelay()
    {
        var handler = new SequenceHandler(["{\"ok\":true}"]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);

        var body = await ThirdPartyHttp.GetStringWithRetryAsync(
            client,
            RequestUri,
            RecordDelay(delays),
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(1, handler.Attempts);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task GetStringWithRetryAsync_RetriesSslFailureFiveTimesThenSucceeds()
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

        var body = await ThirdPartyHttp.GetStringWithRetryAsync(
            client,
            RequestUri,
            RecordDelay(delays),
            CancellationToken.None);

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(6, handler.Attempts);
        Assert.Equal(ExpectedRetryDelays, delays);
    }

    [Fact]
    public async Task GetStringWithRetryAsync_ThrowsAfterFiveRetries()
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

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() =>
            ThirdPartyHttp.GetStringWithRetryAsync(
                client,
                RequestUri,
                RecordDelay(delays),
                CancellationToken.None));

        Assert.Equal(SslFailure, thrown.Message);
        Assert.Equal(6, handler.Attempts);
        Assert.Equal(ExpectedRetryDelays, delays);
    }

    [Fact]
    public async Task GetStringWithRetryAsync_DoesNotRetryWhenCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new SequenceHandler([new HttpRequestException(SslFailure)]);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ThirdPartyHttp.GetStringWithRetryAsync(
                client,
                RequestUri,
                RecordDelay(delays),
                cts.Token));

        Assert.InRange(handler.Attempts, 0, 1);
        Assert.Empty(delays);
    }

    [Theory]
    [InlineData("core-dotnet/core/Geo/Handlers/GetLatLongHandler.cs")]
    [InlineData("core-dotnet/core/Geo/Handlers/GetLocationHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherCurrentHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherForecastHandler.cs")]
    [InlineData("core-dotnet/core/Weather/Handlers/GetPublicWeatherHistoryHandler.cs")]
    public void ThirdPartyHttpsHandlers_UseSharedRetryHelper(string relativePath)
    {
        var source = File.ReadAllText(FindRepoFile(relativePath));
        Assert.Contains("ThirdPartyHttp.GetStringWithRetryAsync", source);
        Assert.DoesNotContain("client.GetStringAsync", source);
    }

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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
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
}
