namespace Poangjakten.Web.Challenges;

public interface IChallengeRepository
{
    Task<IReadOnlyList<Challenge>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(Challenge challenge, CancellationToken cancellationToken);
    Task DeleteAsync(string challengeId, CancellationToken cancellationToken);
}
