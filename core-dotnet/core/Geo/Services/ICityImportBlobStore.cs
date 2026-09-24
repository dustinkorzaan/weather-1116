namespace Core.Geo.Services;

/// <summary>
/// Temp blob files that stage the daily import-cities run: ImportCitiesHandler writes the admin1
/// names, one file per batch of cities, and the imported GeonameIds; the upsert and delete jobs it
/// enqueues read them back by name. Blobs live in the <see cref="ContainerName"/> container.
/// </summary>
public interface ICityImportBlobStore
{
    public const string ContainerName = "temp";

    Task UploadTextAsync(string blobName, string content, CancellationToken cancellationToken);

    Task<string> DownloadTextAsync(string blobName, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}
