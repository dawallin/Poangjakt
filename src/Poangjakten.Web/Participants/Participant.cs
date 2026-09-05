namespace Poangjakten.Web.Participants;

public sealed record Participant(
    string Id,
    string DisplayName,
    string LoginCode,
    string Clue,
    string TableId,
    int Score,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
