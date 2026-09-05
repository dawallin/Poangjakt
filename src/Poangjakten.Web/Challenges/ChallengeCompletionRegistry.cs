using System.Collections.Concurrent;

namespace Poangjakten.Web.Challenges;

public sealed class ChallengeCompletionRegistry(
    IChallengeCompletionRepository repository,
    ILogger<ChallengeCompletionRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTimeOffset>> _byOwner =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var completions = await repository.LoadAllAsync(cancellationToken);
        foreach (var completion in completions)
        {
            OwnerCompletions(completion.OwnerId)[completion.ChallengeId] = completion.CompletedAt;
        }

        logger.LogInformation("Loaded {CompletionCount} challenge completions into memory", completions.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlySet<string> CompletedChallengeIds(string ownerId) =>
        _byOwner.TryGetValue(ownerId, out var completions)
            ? completions.Keys.ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

    public int CountParticipantCompletions(string challengeId) =>
        CountCompletions(challengeId, ownerId => !ownerId.StartsWith("table:", StringComparison.Ordinal));

    public int CountTableCompletions(string challengeId) =>
        CountCompletions(challengeId, ownerId => ownerId.StartsWith("table:", StringComparison.Ordinal));

    public async Task SetAsync(
        string ownerId,
        string challengeId,
        bool isCompleted,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var completions = OwnerCompletions(ownerId);
            if (isCompleted)
            {
                if (completions.ContainsKey(challengeId)) return;
                var completion = new ChallengeCompletion(ownerId, challengeId, DateTimeOffset.UtcNow);
                await repository.SaveAsync(completion, cancellationToken);
                completions[challengeId] = completion.CompletedAt;
            }
            else
            {
                if (!completions.ContainsKey(challengeId)) return;
                await repository.DeleteAsync(ownerId, challengeId, cancellationToken);
                completions.TryRemove(challengeId, out _);
            }
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task RemoveParticipantAsync(string participantId, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            await repository.DeleteAllForOwnerAsync(
                ChallengeCompletionOwners.ForParticipant(participantId),
                cancellationToken);
            _byOwner.TryRemove(ChallengeCompletionOwners.ForParticipant(participantId), out _);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private ConcurrentDictionary<string, DateTimeOffset> OwnerCompletions(string ownerId) =>
        _byOwner.GetOrAdd(
            ownerId,
            _ => new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal));

    private int CountCompletions(string challengeId, Func<string, bool> includeOwner) =>
        _byOwner.Count(entry =>
            includeOwner(entry.Key) && entry.Value.ContainsKey(challengeId));
}
