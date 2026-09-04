using System.Collections.Concurrent;

namespace Poangjakten.Web.Challenges;

public sealed class ChallengeCompletionRegistry(
    IChallengeCompletionRepository repository,
    ILogger<ChallengeCompletionRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTimeOffset>> _byParticipant =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var completions = await repository.LoadAllAsync(cancellationToken);
        foreach (var completion in completions)
        {
            ParticipantCompletions(completion.ParticipantId)[completion.ChallengeId] = completion.CompletedAt;
        }

        logger.LogInformation("Loaded {CompletionCount} challenge completions into memory", completions.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlySet<string> CompletedChallengeIds(string participantId) =>
        _byParticipant.TryGetValue(participantId, out var completions)
            ? completions.Keys.ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

    public async Task SetAsync(
        string participantId,
        string challengeId,
        bool isCompleted,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var completions = ParticipantCompletions(participantId);
            if (isCompleted)
            {
                if (completions.ContainsKey(challengeId)) return;
                var completion = new ChallengeCompletion(participantId, challengeId, DateTimeOffset.UtcNow);
                await repository.SaveAsync(completion, cancellationToken);
                completions[challengeId] = completion.CompletedAt;
            }
            else
            {
                if (!completions.ContainsKey(challengeId)) return;
                await repository.DeleteAsync(participantId, challengeId, cancellationToken);
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
            await repository.DeleteAllForParticipantAsync(participantId, cancellationToken);
            _byParticipant.TryRemove(participantId, out _);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private ConcurrentDictionary<string, DateTimeOffset> ParticipantCompletions(string participantId) =>
        _byParticipant.GetOrAdd(
            participantId,
            _ => new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal));
}
