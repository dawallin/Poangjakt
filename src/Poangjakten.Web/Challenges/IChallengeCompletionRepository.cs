namespace Poangjakten.Web.Challenges;

public interface IChallengeCompletionRepository
{
    Task<IReadOnlyList<ChallengeCompletion>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(ChallengeCompletion completion, CancellationToken cancellationToken);
    Task DeleteAsync(string participantId, string challengeId, CancellationToken cancellationToken);
    Task DeleteAllForParticipantAsync(string participantId, CancellationToken cancellationToken);
}
