namespace Poangjakten.Web.Songs;

public sealed record Song(
    string Id,
    string Title,
    string Melody,
    string Lyrics,
    int SortOrder,
    string? ImageBlobName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
