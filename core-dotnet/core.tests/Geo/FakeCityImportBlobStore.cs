using Core.Geo.Services;

namespace Core.Tests.Geo;

/// <summary>In-memory <see cref="ICityImportBlobStore"/> that records the order of every call.</summary>
internal sealed class FakeCityImportBlobStore : ICityImportBlobStore
{
    public Dictionary<string, string> Blobs { get; } = [];

    public List<string> Calls { get; } = [];

    public Task UploadTextAsync(string blobName, string content, CancellationToken cancellationToken)
    {
        Calls.Add($"upload {blobName}");
        Blobs[blobName] = content;
        return Task.CompletedTask;
    }

    public Task<string> DownloadTextAsync(string blobName, CancellationToken cancellationToken)
    {
        Calls.Add($"download {blobName}");
        return Task.FromResult(Blobs[blobName]);
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        Calls.Add($"delete {blobName}");
        Blobs.Remove(blobName);
        return Task.CompletedTask;
    }
}
