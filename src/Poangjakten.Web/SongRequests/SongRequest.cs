namespace Poangjakten.Web.SongRequests;

public sealed record SongRequest(
    string Id,
    string Artist,
    string Title,
    string TableId,
    string RequestedByParticipantId,
    DateTimeOffset RequestedAt);
