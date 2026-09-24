using Azure.Storage.Blobs;
using Core.AIWeather.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Geo.Services;

/// <summary>
/// <see cref="ICityImportBlobStore"/> on Azure Blob Storage. In Azure the worker reaches the storage
/// account at BLOB_STORAGE_URL with its user-assigned managed identity (AZURE_CLIENT_ID, Storage Blob
/// Data Contributor); locally BLOB_CONNECTION_STRING (e.g. UseDevelopmentStorage=true for Azurite)
/// takes precedence.
/// </summary>
public class AzureCityImportBlobStore : ICityImportBlobStore
{
    private readonly BlobContainerClient _container;
    private readonly Lazy<Task> _created;

    public AzureCityImportBlobStore(BlobContainerClient container)
    {
        _container = container;
        _created = new(() => _container.CreateIfNotExistsAsync());
    }

    public async Task UploadTextAsync(string blobName, string content, CancellationToken cancellationToken)
    {
        await _created.Value;
        await _container.GetBlobClient(blobName).UploadAsync(BinaryData.FromString(content), overwrite: true, cancellationToken);
    }

    public async Task<string> DownloadTextAsync(string blobName, CancellationToken cancellationToken)
    {
        var result = await _container.GetBlobClient(blobName).DownloadContentAsync(cancellationToken);
        return result.Value.Content.ToString();
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken) =>
        await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}

public static class CityImportBlobStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICityImportBlobStore"/> when BLOB_CONNECTION_STRING or BLOB_STORAGE_URL is
    /// set. Only the worker, which runs import-cities, calls this; the import handlers take the store
    /// as optional so every other Core host still passes DI validation without blob settings.
    /// </summary>
    public static IServiceCollection AddCityImportBlobStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["BLOB_CONNECTION_STRING"];
        var storageUrl = configuration["BLOB_STORAGE_URL"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ICityImportBlobStore>(new AzureCityImportBlobStore(
                new BlobContainerClient(connectionString, ICityImportBlobStore.ContainerName)));
        }
        else if (!string.IsNullOrWhiteSpace(storageUrl))
        {
            var containerUri = new Uri($"{storageUrl.TrimEnd('/')}/{ICityImportBlobStore.ContainerName}");
            services.AddSingleton<ICityImportBlobStore>(new AzureCityImportBlobStore(
                new BlobContainerClient(containerUri, FoundryTokenCredentialFactory.Create())));
        }

        return services;
    }
}
