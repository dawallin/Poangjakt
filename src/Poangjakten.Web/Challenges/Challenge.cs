namespace Poangjakten.Web.Challenges;

public sealed record Challenge(
    string Id,
    string Description,
    int Points,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
