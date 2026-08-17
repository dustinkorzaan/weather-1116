using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Core.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Http;

public class TransientRetryHelperTests
{
    private static TransientRetryHelper CreateSut() => new(NullLogger<TransientRetryHelper>.Instance);

    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt()
    {
        var sut = CreateSut();
        var attempts = 0;

        var result = await sut.ExecuteAsync(
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
    public async Task ExecuteAsync_RetriesTransientFailuresThenSucceeds()
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

        var result = await sut.ExecuteAsync(
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
    public async Task ExecuteAsync_ThrowsAfterExhaustingRetries()
    {
        var sut = CreateSut();
        var attempts = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.ExecuteAsync<string>(
                _ =>
                {
                    attempts++;
                    throw new HttpRequestException("always fails");
                },
                CancellationToken.None));

        Assert.Equal(TransientRetryHelper.RetryCount + 1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryNonTransientException()
    {
        var sut = CreateSut();
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync<string>(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("not transient");
                },
                CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryWhenCanceled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync<string>(
                ct =>
                {
                    attempts++;
                    ct.ThrowIfCancellationRequested();
                    throw new HttpRequestException("unreachable");
                },
                cts.Token));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesOnTimeoutThenSucceeds()
    {
        var sut = CreateSut();
        var outcomes = new Queue<Exception?>([new TaskCanceledException("timeout"), null]);
        var attempts = 0;

        var result = await sut.ExecuteAsync(
            _ =>
            {
                attempts++;
                var outcome = outcomes.Dequeue();
                return outcome is null ? Task.FromResult("ok") : throw outcome;
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryTimeoutWhenCallerCanceled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync<string>(
                ct =>
                {
                    attempts++;
                    ct.ThrowIfCancellationRequested();
                    throw new TaskCanceledException("unreachable");
                },
                cts.Token));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsRetryAfterDelayInsteadOfExponentialBackoff()
    {
        var sut = CreateSut();
        var outcomes = new Queue<Exception?>(
            [new RetryAfterException("rate limited", TimeSpan.FromMilliseconds(10)), null]);
        var attempts = 0;

        var stopwatch = Stopwatch.StartNew();
        var result = await sut.ExecuteAsync(
            _ =>
            {
                attempts++;
                var outcome = outcomes.Dequeue();
                return outcome is null ? Task.FromResult("ok") : throw outcome;
            },
            CancellationToken.None);
        stopwatch.Stop();

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
        // The default first backoff is 200ms; a much shorter elapsed time proves the
        // explicit 10ms Retry-After delay was honored instead.
        Assert.True(stopwatch.ElapsedMilliseconds < 150, $"Expected < 150ms, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void EnsureSuccessOrThrowRetryAfter_SuccessStatusCode_DoesNotThrow()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        TransientRetryHelper.EnsureSuccessOrThrowRetryAfter(response);
    }

    [Fact]
    public void EnsureSuccessOrThrowRetryAfter_TooManyRequestsWithRetryAfter_ThrowsWithDelay()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        var exception = Assert.Throws<RetryAfterException>(
            () => TransientRetryHelper.EnsureSuccessOrThrowRetryAfter(response));

        Assert.Equal(TimeSpan.FromSeconds(5), exception.RetryAfter);
    }

    [Fact]
    public void EnsureSuccessOrThrowRetryAfter_ErrorWithoutRetryAfter_ThrowsPlainHttpRequestException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var exception = Assert.Throws<HttpRequestException>(
            () => TransientRetryHelper.EnsureSuccessOrThrowRetryAfter(response));

        Assert.IsNotType<RetryAfterException>(exception);
    }
}
