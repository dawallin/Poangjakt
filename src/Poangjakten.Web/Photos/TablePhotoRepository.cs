using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Photos;

public sealed class TablePhotoRepository(AzureStorageClients storage) : IPhotoRepository
{
    private const string PartitionKey = "photo";

    public async Task<IReadOnlyList<Photo>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.PhotosTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var photos = new List<Photo>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            photos.Add(ToPhoto(entity));
        }

        return photos;
    }

    public async Task SaveAsync(Photo photo, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(PartitionKey, photo.Id)
        {
            ["ParticipantId"] = photo.ParticipantId,
            ["PhotographerDisplayName"] = photo.PhotographerDisplayName,
            ["ImageBlobName"] = photo.ImageBlobName,
            ["ThumbnailBlobName"] = photo.ThumbnailBlobName,
            ["ImageBytes"] = photo.ImageBytes,
            ["ThumbnailBytes"] = photo.ThumbnailBytes,
            ["UploadedAt"] = photo.UploadedAt
        };

        await storage.PhotosTable().UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string photoId, CancellationToken cancellationToken)
    {
        await storage.PhotosTable()
            .DeleteEntityAsync(PartitionKey, photoId, cancellationToken: cancellationToken);
    }

    private static Photo ToPhoto(TableEntity entity) => new(
        entity.RowKey,
        entity.GetString("ParticipantId") ?? "",
        entity.GetString("PhotographerDisplayName") ?? "Okänd fotograf",
        entity.GetString("ImageBlobName") ?? $"photos/{entity.RowKey}.jpg",
        entity.GetString("ThumbnailBlobName") ?? $"photos/thumbnails/{entity.RowKey}.jpg",
        entity.GetInt64("ImageBytes") ?? 0,
        entity.GetInt64("ThumbnailBytes") ?? 0,
        entity.GetDateTimeOffset("UploadedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
