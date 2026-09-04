using Azure.Storage.Blobs.Models;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Photos;

public sealed class PhotoBlobStore(AzureStorageClients storage)
{
    private static readonly BlobHttpHeaders JpegHeaders = new()
    {
        ContentType = "image/jpeg",
        CacheControl = "public, max-age=31536000, immutable"
    };

    public async Task UploadAsync(
        Photo photo,
        Stream image,
        Stream thumbnail,
        CancellationToken cancellationToken)
    {
        var container = storage.PhotoContainer();
        var imageBlob = container.GetBlobClient(photo.ImageBlobName);
        var thumbnailBlob = container.GetBlobClient(photo.ThumbnailBlobName);

        try
        {
            await imageBlob.UploadAsync(image, new BlobUploadOptions { HttpHeaders = JpegHeaders }, cancellationToken);
            await thumbnailBlob.UploadAsync(thumbnail, new BlobUploadOptions { HttpHeaders = JpegHeaders }, cancellationToken);
        }
        catch
        {
            try
            {
                await Task.WhenAll(
                    imageBlob.DeleteIfExistsAsync(cancellationToken: CancellationToken.None),
                    thumbnailBlob.DeleteIfExistsAsync(cancellationToken: CancellationToken.None));
            }
            catch
            {
                // Preserve the original upload failure; orphan cleanup is best effort.
            }
            throw;
        }
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

    public async Task DeleteAsync(Photo photo, CancellationToken cancellationToken)
    {
        var container = storage.PhotoContainer();
        await Task.WhenAll(
            container.GetBlobClient(photo.ImageBlobName).DeleteIfExistsAsync(cancellationToken: cancellationToken),
            container.GetBlobClient(photo.ThumbnailBlobName).DeleteIfExistsAsync(cancellationToken: cancellationToken));
    }
}
