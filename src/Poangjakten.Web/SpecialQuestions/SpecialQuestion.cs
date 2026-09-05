using Poangjakten.Web.PartyStages;

namespace Poangjakten.Web.SpecialQuestions;

public sealed record SpecialQuestionDefinition(
    string Id,
    string Prompt,
    string UnlockStageId)
{
    public int PointsFor(int value) => value / 10;
}

public static class SpecialQuestionDefinitions
{
    public const string DanielPercentageId = "daniel-percentage";

    public static readonly IReadOnlyList<SpecialQuestionDefinition> All =
    [
        new(
            DanielPercentageId,
            "Hur många % Daniel är du?",
            PartyStageDefinitions.AfterDanielPercentageId)
    ];

    private static readonly IReadOnlyDictionary<string, SpecialQuestionDefinition> ById =
        All.ToDictionary(question => question.Id, StringComparer.Ordinal);

    public static SpecialQuestionDefinition? Find(string? id) =>
        id is not null && ById.TryGetValue(id, out var question) ? question : null;
}

public sealed record SpecialAnswer(
    string ParticipantId,
    string QuestionId,
    int Value,
    DateTimeOffset UpdatedAt);
