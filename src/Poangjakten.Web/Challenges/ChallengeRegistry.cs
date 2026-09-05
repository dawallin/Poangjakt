using System.Collections.Concurrent;
using Poangjakten.Web.PartyStages;

namespace Poangjakten.Web.Challenges;

public sealed class ChallengeRegistry(
    IChallengeRepository repository,
    ILogger<ChallengeRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, Challenge> _challenges = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var challenges = await repository.LoadAllAsync(cancellationToken);
        foreach (var challenge in challenges)
        {
            _challenges[challenge.Id] = challenge;
        }

        logger.LogInformation("Loaded {ChallengeCount} challenges into memory", challenges.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlyList<Challenge> List() => _challenges.Values
        .OrderBy(challenge => challenge.Points)
        .ThenBy(challenge => challenge.Description, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public Challenge? Find(string id) => _challenges.GetValueOrDefault(id);

    public async Task<ChallengeMutationResult> CreateAsync(
        string? description,
        int points,
        string? scope,
        string? unlockStageId,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(
            description,
            points,
            scope,
            unlockStageId,
            out var normalizedDescription,
            out var normalizedScope,
            out var normalizedUnlockStageId);
        if (validationError is not null)
        {
            return ChallengeMutationResult.Invalid(validationError);
        }

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var challenge = new Challenge(
                Guid.NewGuid().ToString("N"),
                normalizedDescription!,
                points,
                normalizedScope!,
                normalizedUnlockStageId,
                now,
                now);
            await repository.SaveAsync(challenge, cancellationToken);
            _challenges[challenge.Id] = challenge;
            return ChallengeMutationResult.Success(challenge);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<ChallengeMutationResult> UpdateAsync(
        string id,
        string? description,
        int points,
        string? scope,
        string? unlockStageId,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(
            description,
            points,
            scope,
            unlockStageId,
            out var normalizedDescription,
            out var normalizedScope,
            out var normalizedUnlockStageId);
        if (validationError is not null)
        {
            return ChallengeMutationResult.Invalid(validationError);
        }

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_challenges.TryGetValue(id, out var existing))
            {
                return ChallengeMutationResult.NotFound();
            }

            var updated = existing with
            {
                Description = normalizedDescription!,
                Points = points,
                Scope = normalizedScope!,
                UnlockStageId = normalizedUnlockStageId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await repository.SaveAsync(updated, cancellationToken);
            _challenges[id] = updated;
            return ChallengeMutationResult.Success(updated);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_challenges.ContainsKey(id))
            {
                return false;
            }

            await repository.DeleteAsync(id, cancellationToken);
            _challenges.TryRemove(id, out _);
            return true;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static string? Validate(
        string? description,
        int points,
        string? scope,
        string? unlockStageId,
        out string? normalizedDescription,
        out string? normalizedScope,
        out string? normalizedUnlockStageId)
    {
        normalizedDescription = description?.Trim();
        normalizedScope = ChallengeScopes.Normalize(scope);
        normalizedUnlockStageId = string.IsNullOrWhiteSpace(unlockStageId)
            ? null
            : PartyStageDefinitions.Find(unlockStageId)?.Id;
        if (normalizedDescription?.Length is not (>= 3 and <= 240))
        {
            return "Beskrivningen måste vara 3–240 tecken.";
        }

        if (points is < 1 or > 1000) return "Poängen måste vara mellan 1 och 1000.";
        if (normalizedScope is null) return "Välj om uppgiften gäller en individ eller ett bord.";
        return !string.IsNullOrWhiteSpace(unlockStageId) && normalizedUnlockStageId is null
            ? "Välj ett giltigt feststeg."
            : null;
    }
}

public sealed record ChallengeMutationResult(Challenge? Challenge, string? Error, bool WasNotFound)
{
    public static ChallengeMutationResult Success(Challenge challenge) => new(challenge, null, false);
    public static ChallengeMutationResult Invalid(string error) => new(null, error, false);
    public static ChallengeMutationResult NotFound() => new(null, null, true);
}
