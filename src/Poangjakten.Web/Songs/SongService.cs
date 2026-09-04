namespace Poangjakten.Web.Songs;

public sealed class SongService(
    SongRegistry songs,
    SongBlobStore blobs,
    ILogger<SongService> logger)
{
    public async Task<Song?> SetImageAsync(
        string id,
        Stream image,
        CancellationToken cancellationToken)
    {
        var existing = songs.Find(id);
        if (existing is null)
        {
            return null;
        }

        var newBlobName = $"songs/{id}/{Guid.NewGuid():N}.jpg";
        await blobs.UploadAsync(newBlobName, image, cancellationToken);

        Song? updated;
        try
        {
            updated = await songs.SetImageAsync(id, newBlobName, cancellationToken);
        }
        catch
        {
            await TryDeleteBlobAsync(newBlobName);
            throw;
        }

        if (updated is null)
        {
            await TryDeleteBlobAsync(newBlobName);
            return null;
        }

        if (existing.ImageBlobName is not null)
        {
            await TryDeleteBlobAsync(existing.ImageBlobName);
        }

        return updated;
    }

    public async Task<bool> RemoveImageAsync(string id, CancellationToken cancellationToken)
    {
        var existing = songs.Find(id);
        if (existing is null)
        {
            return false;
        }

        if (existing.ImageBlobName is null)
        {
            return true;
        }

        var updated = await songs.SetImageAsync(id, null, cancellationToken);
        if (updated is null)
        {
            return false;
        }

        await TryDeleteBlobAsync(existing.ImageBlobName);
        return true;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var removed = await songs.RemoveAsync(id, cancellationToken);
        if (removed is null)
        {
            return false;
        }

        if (removed.ImageBlobName is not null)
        {
            await TryDeleteBlobAsync(removed.ImageBlobName);
        }

        return true;
    }

    private async Task TryDeleteBlobAsync(string blobName)
    {
        try
        {
            await blobs.DeleteAsync(blobName, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not delete song image blob {BlobName}", blobName);
        }
    }
}
