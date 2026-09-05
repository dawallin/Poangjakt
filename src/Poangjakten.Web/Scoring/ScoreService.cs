using Poangjakten.Web.Challenges;
using Poangjakten.Web.Participants;

namespace Poangjakten.Web.Scoring;

public sealed class ScoreService(
    ChallengeRegistry challenges,
    ChallengeCompletionRegistry completions)
{
    public int GetScore(Participant participant)
    {
        var challengeScore = completions.CompletedChallengeIds(
                ChallengeCompletionOwners.ForParticipant(participant.Id))
            .Select(challenges.Find)
            .Where(challenge => challenge?.Scope == ChallengeScopes.Individual)
            .Sum(challenge => challenge!.Points);

        // Participant.Score is retained as a base/adjustment score so more point
        // sources (Daniel-test, table bonuses, manual adjustments) can be added later.
        return participant.Score + challengeScore;
    }

    public int GetTableScore(string tableId) => completions
        .CompletedChallengeIds(ChallengeCompletionOwners.ForTable(tableId))
        .Select(challenges.Find)
        .Where(challenge => challenge?.Scope == ChallengeScopes.Table)
        .Sum(challenge => challenge!.Points);
}
