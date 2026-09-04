namespace Poangjakten.Web.Photos;

public sealed record Photo(
    string Id,
    string ParticipantId,
    string PhotographerDisplayName,
    string ImageBlobName,
    string ThumbnailBlobName,
    long ImageBytes,
    long ThumbnailBytes,
    DateTimeOffset UploadedAt);
