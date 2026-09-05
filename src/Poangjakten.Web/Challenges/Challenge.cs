namespace Poangjakten.Web.Challenges;

public sealed record Challenge(
    string Id,
    string Description,
    int Points,
    string Scope,
    string? UnlockStageId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class ChallengeScopes
{
    public const string Individual = "individual";
    public const string Table = "table";

    public static string? Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        Individual => Individual,
        Table => Table,
        _ => null
    };
}
