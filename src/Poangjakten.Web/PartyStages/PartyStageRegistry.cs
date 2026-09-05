using System.Collections.Concurrent;

namespace Poangjakten.Web.PartyStages;

public sealed class PartyStageRegistry(
    IPartyStageRepository repository,
    ILogger<PartyStageRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, PartyStageState> _states = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var storedStates = await repository.LoadAllAsync(cancellationToken);
        foreach (var state in storedStates.Where(state => PartyStageDefinitions.Find(state.Id) is not null))
        {
            _states[state.Id] = state;
        }

        logger.LogInformation("Loaded {PartyStageCount} party stage states into memory", storedStates.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public bool IsUnlocked(string id) =>
        _states.TryGetValue(id, out var state) && state.IsUnlocked;

    public IReadOnlyList<PartyStageStatus> List() => PartyStageDefinitions.All
        .Select(definition => new PartyStageStatus(
            definition,
            IsUnlocked(definition.Id),
            _states.GetValueOrDefault(definition.Id)?.UpdatedAt))
        .ToArray();

    public async Task<PartyStageStatus?> SetUnlockedAsync(
        string id,
        bool isUnlocked,
        CancellationToken cancellationToken)
    {
        var definition = PartyStageDefinitions.Find(id);
        if (definition is null) return null;

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var state = new PartyStageState(id, isUnlocked, DateTimeOffset.UtcNow);
            await repository.SaveAsync(state, cancellationToken);
            _states[id] = state;
            logger.LogInformation("Party stage {PartyStageId} changed to {IsUnlocked}", id, isUnlocked);
            return new PartyStageStatus(definition, isUnlocked, state.UpdatedAt);
        }
        finally
        {
            _mutationLock.Release();
        }
    }
}

public sealed record PartyStageStatus(
    PartyStageDefinition Definition,
    bool IsUnlocked,
    DateTimeOffset? UpdatedAt);
