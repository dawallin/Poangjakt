namespace Poangjakten.Web.Participants;

public sealed record Participant(
    string Id,
    string DisplayName,
    int Score,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
