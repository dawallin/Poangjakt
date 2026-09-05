namespace Poangjakten.Web.Challenges;

public interface IChallengeCompletionRepository
{
    Task<IReadOnlyList<ChallengeCompletion>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(ChallengeCompletion completion, CancellationToken cancellationToken);
    Task DeleteAsync(string ownerId, string challengeId, CancellationToken cancellationToken);
    Task DeleteAllForOwnerAsync(string ownerId, CancellationToken cancellationToken);
}
