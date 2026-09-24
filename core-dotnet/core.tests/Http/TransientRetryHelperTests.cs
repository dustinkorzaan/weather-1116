using System.Net;
using System.Net.Sockets;
using Core.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Http;

public class TransientRetryHelperTests
{
    private static TransientRetryHelper CreateSut() => new(NullLogger<TransientRetryHelper>.Instance);

    [Fact]
    public async Task Execute_SucceedsOnFirstAttempt()
    {
        var sut = CreateSut();
        var attempts = 0;

        var result = await sut.Execute(
            _ =>
            {
                attempts++;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Execute_RetriesTransientFailuresThenSucceeds()
    {
        var sut = CreateSut();
        var outcomes = new Queue<Exception?>(
            [
                new HttpRequestException("first"),
                new IOException("second"),
                new SocketException(),
                null,
            ]);
        var attempts = 0;

        var result = await sut.Execute(
            _ =>
            {
                attempts++;
                var outcome = outcomes.Dequeue();
                return outcome is null ? Task.FromResult("ok") : throw outcome;
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(4, attempts);
    }

    [Fact]
    public async Task Execute_ThrowsAfterExhaustingRetries()
    {
        var sut = CreateSut();
        var attempts = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.Execute<string>(
                _ =>
                {
                    attempts++;
                    throw new HttpRequestException("always fails");
                },
                CancellationToken.None));

        Assert.Equal(TransientRetryHelper.RetryCount + 1, attempts);
    }

    [Fact]
    public async Task Execute_DoesNotRetryNonTransientException()
    {
        var sut = CreateSut();
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Execute<string>(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("not transient");
                },
                CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Execute_DoesNotRetryPermanentHttpStatus(HttpStatusCode statusCode)
    {
        var sut = CreateSut();
        var attempts = 0;

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.Execute<string>(
                _ =>
                {
                    attempts++;
                    throw new HttpRequestException("rejected", null, statusCode);
                },
                CancellationToken.None));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Execute_RetriesThrottlingAndServerErrorStatus(HttpStatusCode statusCode)
    {
        var sut = CreateSut();
        var attempts = 0;

        var result = await sut.Execute(
            _ =>
            {
                attempts++;
                return attempts == 1
                    ? throw new HttpRequestException("try again", null, statusCode)
                    : Task.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    public void IsPermanentFailure_ClassifiesStatusCodes(HttpStatusCode statusCode, bool expected) =>
        Assert.Equal(expected, TransientRetryHelper.IsPermanentFailure(statusCode));

    [Fact]
    public async Task Execute_DoesNotRetryWhenCanceled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.Execute<string>(
                ct =>
                {
                    attempts++;
                    ct.ThrowIfCancellationRequested();
                    throw new HttpRequestException("unreachable");
                },
                cts.Token));

        Assert.Equal(1, attempts);
    }
}
