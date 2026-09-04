using Poangjakten.Web.Participants;

namespace Poangjakten.Web.Photos;

public sealed class PhotoService(
    ParticipantRegistry participants,
    PhotoRegistry photos,
    PhotoBlobStore blobs,
    ILogger<PhotoService> logger)
{
    public async Task<PhotoUploadResult> UploadAsync(
        string participantId,
        Stream image,
        long imageBytes,
        Stream thumbnail,
        long thumbnailBytes,
        CancellationToken cancellationToken)
    {
        var participant = participants.Find(participantId);
        if (participant is null)
        {
            return PhotoUploadResult.Invalid("Deltagaren finns inte längre. Logga in igen.");
        }

        var id = Guid.NewGuid().ToString("N");
        var photo = new Photo(
            id,
            participant.Id,
            participant.DisplayName,
            $"photos/{id}.jpg",
            $"photos/thumbnails/{id}.jpg",
            imageBytes,
            thumbnailBytes,
            DateTimeOffset.UtcNow);

        await blobs.UploadAsync(photo, image, thumbnail, cancellationToken);
        try
        {
            await photos.AddAsync(photo, cancellationToken);
        }
        catch
        {
            try
            {
                await blobs.DeleteAsync(photo, CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not clean up blobs after metadata failure for photo {PhotoId}", photo.Id);
            }
            throw;
        }

        logger.LogInformation("Participant {ParticipantId} uploaded photo {PhotoId}", participant.Id, photo.Id);
        return PhotoUploadResult.Success(photo);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var photo = await photos.RemoveAsync(id, cancellationToken);
        if (photo is null)
        {
            return false;
        }

        try
        {
            await blobs.DeleteAsync(photo, cancellationToken);
        }
        catch (Exception exception)
        {
            // The photo is already hidden from the app. Keep deletion successful and
            // log any orphaned blobs so cleanup can be retried operationally.
            logger.LogError(exception, "Could not delete blobs for removed photo {PhotoId}", photo.Id);
        }

        return true;
    }

    public async Task<OwnedPhotoDeleteResult> DeleteOwnedAsync(
        string participantId,
        string photoId,
        CancellationToken cancellationToken)
    {
        var photo = photos.Find(photoId);
        if (photo is null)
        {
            return OwnedPhotoDeleteResult.NotFound;
        }

        if (!string.Equals(photo.ParticipantId, participantId, StringComparison.Ordinal))
        {
            return OwnedPhotoDeleteResult.Forbidden;
        }

        return await DeleteAsync(photoId, cancellationToken)
            ? OwnedPhotoDeleteResult.Deleted
            : OwnedPhotoDeleteResult.NotFound;
    }
}

public enum OwnedPhotoDeleteResult
{
    Deleted,
    NotFound,
    Forbidden
}

public sealed record PhotoUploadResult(Photo? Photo, string? Error)
{
    public static PhotoUploadResult Success(Photo photo) => new(photo, null);
    public static PhotoUploadResult Invalid(string error) => new(null, error);
}
