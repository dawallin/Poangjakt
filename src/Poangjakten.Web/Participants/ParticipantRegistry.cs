using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Poangjakten.Web.Participants;

public sealed partial class ParticipantRegistry(
    IParticipantRepository repository,
    ILogger<ParticipantRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, Participant> _participants = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _idsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var participants = await repository.LoadAllAsync(cancellationToken);
        foreach (var participant in participants)
        {
            _participants[participant.Id] = participant;
            _idsByName.TryAdd(participant.DisplayName, participant.Id);
        }

        logger.LogInformation("Loaded {ParticipantCount} participants into memory", participants.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Participant? Find(string id) => _participants.GetValueOrDefault(id);

    public IReadOnlyList<Participant> List() => _participants.Values
        .OrderByDescending(participant => participant.Score)
        .ThenBy(participant => participant.DisplayName, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public async Task<RegistrationResult> RegisterAsync(string? requestedName, CancellationToken cancellationToken)
    {
        var displayName = NormalizeName(requestedName);
        if (displayName is null)
        {
            return RegistrationResult.Invalid("Skriv ett namn med 2–50 tecken.");
        }

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (_idsByName.TryGetValue(displayName, out var existingId) &&
                _participants.TryGetValue(existingId, out var existing))
            {
                return RegistrationResult.Existing(existing);
            }

            var now = DateTimeOffset.UtcNow;
            var participant = new Participant(
                Guid.NewGuid().ToString("N"),
                displayName,
                0,
                now,
                now);

            // Persist first so the in-memory state can never contain a participant
            // that would disappear after an application restart.
            await repository.SaveAsync(participant, cancellationToken);
            _participants[participant.Id] = participant;
            _idsByName[participant.DisplayName] = participant.Id;

            logger.LogInformation("Registered participant {ParticipantId}", participant.Id);
            return RegistrationResult.Created(participant);
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
            if (!_participants.TryGetValue(id, out var participant))
            {
                return false;
            }

            // Delete the durable state first. If Azure rejects the operation,
            // the participant remains available in memory and can be retried.
            await repository.DeleteAsync(id, cancellationToken);
            _participants.TryRemove(id, out _);
            _idsByName.TryRemove(participant.DisplayName, out _);
            logger.LogInformation("Removed participant {ParticipantId}", id);
            return true;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static string? NormalizeName(string? value)
    {
        var normalized = Whitespace().Replace(value?.Trim() ?? "", " ");
        return normalized.Length is >= 2 and <= 50 ? normalized : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

public sealed record RegistrationResult(Participant? Participant, bool WasCreated, string? Error)
{
    public static RegistrationResult Created(Participant participant) => new(participant, true, null);
    public static RegistrationResult Existing(Participant participant) => new(participant, false, null);
    public static RegistrationResult Invalid(string error) => new(null, false, error);
}
