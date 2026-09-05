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
    public const string AfterDinnerId = "after-dinner";

    public static readonly IReadOnlyList<PartyStageDefinition> All =
    [
        new(
            TableRevealId,
            "Visa borden",
            "Deltagarna kan se sitt bord och använda Låtlista, bordspoängjakten och Topplista Bord."),
        new(
            AfterDinnerId,
            "Efter maten",
            "Poänguppgifter märkta Efter maten blir synliga för deltagarna.")
    ];

    private static readonly IReadOnlyDictionary<string, PartyStageDefinition> ById =
        All.ToDictionary(stage => stage.Id, StringComparer.Ordinal);

    public static PartyStageDefinition? Find(string? id) =>
        id is not null && ById.TryGetValue(id, out var stage) ? stage : null;
}
