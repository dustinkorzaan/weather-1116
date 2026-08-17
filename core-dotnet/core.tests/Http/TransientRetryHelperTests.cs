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
}
