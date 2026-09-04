using Azure.Storage.Blobs.Models;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Songs;

public sealed class SongBlobStore(AzureStorageClients storage)
{
    public async Task UploadAsync(string blobName, Stream image, CancellationToken cancellationToken)
    {
        await storage.PhotoContainer().GetBlobClient(blobName).UploadAsync(
            image,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg",
                    CacheControl = "public, max-age=31536000, immutable"
                }
            },
            cancellationToken);
    }

    public async Task<BlobDownloadStreamingResult> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        var response = await storage.PhotoContainer()
            .GetBlobClient(blobName)
            .DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        await storage.PhotoContainer().GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
