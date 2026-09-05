using Poangjakten.Web.Challenges;
using Poangjakten.Web.Participants;
using Poangjakten.Web.PartyStages;
using Poangjakten.Web.SpecialQuestions;

namespace Poangjakten.Web.Scoring;

public sealed class ScoreService(
    ChallengeRegistry challenges,
    ChallengeCompletionRegistry completions,
    SpecialAnswerRegistry specialAnswers,
    PartyStageRegistry stages)
{
    public int GetScore(Participant participant)
    {
        var challengeScore = completions.CompletedChallengeIds(
                ChallengeCompletionOwners.ForParticipant(participant.Id))
            .Select(challenges.Find)
            .Where(challenge => challenge?.Scope == ChallengeScopes.Individual && IsVisible(challenge))
            .Sum(challenge => challenge!.Points);

        // Participant.Score is retained as a base/adjustment score so more point
        // sources (Daniel-test, table bonuses, manual adjustments) can be added later.
        var specialQuestionScore = SpecialQuestionDefinitions.All
            .Where(question => stages.IsUnlocked(question.UnlockStageId))
            .Sum(question => specialAnswers.GetPoints(participant.Id, question));

        return participant.Score + challengeScore + specialQuestionScore;
    }

    public int GetTableScore(string tableId) => completions
        .CompletedChallengeIds(ChallengeCompletionOwners.ForTable(tableId))
        .Select(challenges.Find)
        .Where(challenge => challenge?.Scope == ChallengeScopes.Table && IsVisible(challenge))
        .Sum(challenge => challenge!.Points);

    private bool IsVisible(Challenge? challenge) =>
        challenge is not null &&
        (challenge.UnlockStageId is null || stages.IsUnlocked(challenge.UnlockStageId));
}
