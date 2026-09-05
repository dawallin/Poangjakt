namespace Poangjakten.Web.PartyStages;

public sealed record PartyStageDefinition(
    string Id,
    string DisplayName,
    string Description);

public sealed record PartyStageState(
    string Id,
    bool IsUnlocked,
    DateTimeOffset UpdatedAt);

public static class PartyStageDefinitions
{
    public const string TableRevealId = "table-reveal";

    public static readonly IReadOnlyList<PartyStageDefinition> All =
    [
        new(
            TableRevealId,
            "Visa borden",
            "Deltagarna kan se sitt bord och använda bordspoängjakten och Topplista Bord.")
    ];

    private static readonly IReadOnlyDictionary<string, PartyStageDefinition> ById =
        All.ToDictionary(stage => stage.Id, StringComparer.Ordinal);

    public static PartyStageDefinition? Find(string? id) =>
        id is not null && ById.TryGetValue(id, out var stage) ? stage : null;
}
